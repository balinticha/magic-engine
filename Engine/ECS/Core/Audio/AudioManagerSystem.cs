using System;
using System.Collections.Generic;
using DefaultEcs;
using MagicEngine.Engine.Base.Debug;
using MagicEngine.Engine.Base.EntitySystem;
using MagicEngine.Engine.Base.EntityWrappers;
using MagicEngine.Engine.ECS.Core.Audio.Components;
using MagicEngine.Engine.ECS.Core.Events.EntityDeath;
using MagicEngine.Engine.ECS.Core.Positioning.Components;
using MagicEngine.Engine.ECS.Core.Session;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace MagicEngine.Engine.ECS.Core.Audio;

// Every frame!
[UpdateInBucket(ExecutionBucket.Audio)]
public class AudioManagerSystem : EntitySystem
{
    [Dependency] private readonly SessionManager _sessionManager = null!;
    static readonly Random _rng = new Random();

    public Dictionary<string, SoundEffect> SoundBank = new();
    private readonly HashSet<string> _missingSounds = new();
    
    private SoundEffect? GetSoundEffect(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (_missingSounds.Contains(name)) return null;

        if (SoundBank.TryGetValue(name, out var sfx)) return sfx;

        try
        {
            sfx = Content.Load<SoundEffect>(name);
            SoundBank[name] = sfx;
            return sfx;
        }
        catch
        {
            Log($"Failed to load sound: {name}", LogLevel.Release);
            _missingSounds.Add(name);
            return null;
        }
    }
    
    private readonly Queue<PlaySoundRequest> _requestQueue = new();
    private readonly List<ActiveOneShot> _activeOneShots = new();  // current playing one-off sfx
    private readonly List<VoiceCandidate> _candidates = new();  // we reuse this to reduce GC pressure

    private const int MaxVoices = 64;
    // todo unhardcode this one the renderer is rewritten to support literally anything else,
    // and therefore can provide the required values with dependency injection. In the meantime
    // this'll have to do.
    private const float PanRange = 640f / 2f;  // Primary render target width

    private EntitySet? _query;

    public override void OnSceneLoad()
    {
        _query = World.GetEntities().With<ContinousSoundEmitter>().With<RenderPosition>().AsSet();
        
        Events.Subscribe<ContinousSoundEmitter, EntityDeathEvent>(OnEntityDeath);
        PreloadAllSounds();
    }
    
    private void PreloadAllSounds()
    {
        Log("Preloading sounds...", LogLevel.Release);
        var contentRoot = Content.RootDirectory;
        if (!System.IO.Directory.Exists(contentRoot)) return;

        int count = 0;
        
        // Recursive scan
        LoadSoundsFromDir(contentRoot, contentRoot, ref count);
        
        Log($"Preloaded {count} sounds.", LogLevel.Release);
    }

    // i have no idea what this AI-garbage does but it does fix stutter.
    private void LoadSoundsFromDir(string rootDir, string currentDir, ref int count)
    {
        foreach (var file in System.IO.Directory.GetFiles(currentDir, "*.xnb"))
        {
            if (IsSoundEffect(file))
            {
                // Convert absolute path to content-relative path
                // "Content/Sounds/mysound.xnb" -> "Sounds/mysound"
                var relativePath = System.IO.Path.GetRelativePath(rootDir, file);
                var assetName = System.IO.Path.ChangeExtension(relativePath, null);
                
                // Load it!
                GetSoundEffect(assetName);
                count++;
            }
        }

        foreach (var dir in System.IO.Directory.GetDirectories(currentDir))
        {
            LoadSoundsFromDir(rootDir, dir, ref count);
        }
    }
    
    /// <summary>
    /// Reads the first few bytes of the XNB file to determine if it is a SoundEffect.
    /// This prevents us from trying to load Textures as Sounds.
    /// </summary>
    private bool IsSoundEffect(string filePath)
    {
        try
        {
            using (var stream = System.IO.File.OpenRead(filePath))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                // XNB Header Spec:
                // Byte 0-2: 'X', 'N', 'B'
                // Byte 3: Target Platform (w=Windows, m=WindowsPhone7, x=Xbox360, a=Android, i=iOS, g=MacOSX/DesktopGL?)
                // Byte 4: XNB Version (usually 5)
                // Byte 5: Flags (bit 0 = HIdef, bit 7 = Compressed)
                // Byte 6-9: Compressed/Uncompressed Size
                
                // Read first 2kb (header area)
                byte[] buffer = new byte[2048];
                int read = stream.Read(buffer, 0, buffer.Length);
                string headerContent = System.Text.Encoding.ASCII.GetString(buffer, 0, read);
                
                return headerContent.Contains("SoundEffectReader") || headerContent.Contains("SoundEffect");
            }
        }
        catch
        {
            return false;
        }
    }
    
    public override void OnSceneUnload()
    {
        // Todo unsubscibe?
        _query.Dispose();
        ClearSoundQueue();
        StopAllSounds();
    }

    private void OnEntityDeath(Entity<ContinousSoundEmitter> entity, EntityDeathEvent ev)
    {
        ref var emitter = ref entity.Comp;
        if (emitter.ActiveInstance != null)
        {
            emitter.ActiveInstance.Stop();
            emitter.ActiveInstance.Dispose();
            emitter.ActiveInstance = null;
        }
    }

    public override void Update(Timing timing)
    {
        // cleanup and stuff
        for (var i = _activeOneShots.Count - 1; i >= 0; i--)
        {
            if (_activeOneShots[i].Instance.State == SoundState.Stopped)
            {
                _activeOneShots[i].Instance.Dispose();
                _activeOneShots.RemoveAt(i);
            }
        }
        
        // Don't play sound if we, for some reason, don't have an entity controlled
        if (!_sessionManager.GetControlledEntity(out var controlledEntity) ||
            !controlledEntity.TryGet<SoundListener>(out var listener) ||
            !controlledEntity.TryGet<RenderPosition>(out var listenerPosCmp))
        {
            StopAllSounds();
            _requestQueue.Clear();
            return;
        }

        Vector2 listenerPos = listenerPosCmp.Comp.Value;
        var masterVolume = listener.Comp.MasterVolume;
        
        _candidates.Clear();
        
        // handle passive emitters
        foreach (ref readonly var entity in _query.GetEntities())
        {
            // ve use RenderPosition because ExecutionBucket.Audio runs once per frame and therefore we are
            // in a Draw() call
            var pos = entity.Get<RenderPosition>().Value;
            ref var emitter = ref entity.Get<ContinousSoundEmitter>();
            
            // culling
            float distSq = Vector2.DistanceSquared(pos, listenerPos);
            if (emitter.IsPositional && distSq > emitter.Range * emitter.Range)
            {
                if (emitter.ActiveInstance != null && emitter.ActiveInstance.State == SoundState.Playing)
                {
                    emitter.ActiveInstance.Stop();
                }
                continue;
            }
            
            float dist = (float)Math.Sqrt(distSq);
            float finalVol = CalculateVolume(emitter.Volume, dist, emitter.Range, masterVolume, emitter.IsPositional);
            
            _candidates.Add(new VoiceCandidate
            {
                Type = CandidateType.Emitter,
                Importance = CalculateImportance(finalVol, emitter.Importance, true),
                EmitterEntity = entity,
                WorldPosition = pos
            });
        }
        
        // handle new sfx requests
        while (_requestQueue.TryDequeue(out var request))
        {
            float distSq = request.IsGlobal ? 0f: Vector2.DistanceSquared(request.WorldPosition, listenerPos);
            if (!request.IsGlobal && distSq > request.Range * request.Range)
            {
                continue;
            }

            var dist = (float)Math.Sqrt(distSq);
            float vol = CalculateVolume(request.Volume, dist, request.Range, masterVolume, request.IsGlobal);
            
            _candidates.Add(new VoiceCandidate
            {
                Type = CandidateType.NewRequest,
                Importance = CalculateImportance(vol, request.Priority, false),
                RequestData = request,
                WorldPosition = request.WorldPosition
            });
        }

        foreach (var activeShot in _activeOneShots)
        {
            float distSq = activeShot.IsGlobal ? 0f : Vector2.DistanceSquared(activeShot.Position, listenerPos);
            if (!activeShot.IsGlobal && distSq > activeShot.Range * activeShot.Range)
            {
                activeShot.Instance.Stop();  // will be cleaned up next frame
                continue;
            }
            
            float dist = (float)Math.Sqrt(distSq);
            float finalVol = CalculateVolume(activeShot.BaseVolume, dist, activeShot.Range, masterVolume, activeShot.IsGlobal);
            
            _candidates.Add(new VoiceCandidate
            {
                Type = CandidateType.ActiveOneShot,
                Importance = CalculateImportance(finalVol, activeShot.Priority, false),
                ActiveReference = activeShot,
                WorldPosition = activeShot.Position
            });
        }
        
        // uh, is this efficient for every frame?
        // probably fine for now, but TODO this should be a primary candidate for optimization passes
        _candidates.Sort((a, b) => b.Importance.CompareTo(a.Importance));
        
        // We MUST stop the losers (index >= MaxVoices) BEFORE we play the winners.
        for (int i = MaxVoices; i < _candidates.Count; i++)
        {
            var candidate = _candidates[i];
            
            switch (candidate.Type)
            {
                case CandidateType.Emitter:
                    ref var em = ref candidate.EmitterEntity.Get<ContinousSoundEmitter>();
                    if (em.ActiveInstance?.State == SoundState.Playing) em.ActiveInstance.Stop();
                    break;
                case CandidateType.ActiveOneShot:
                    candidate.ActiveReference.Instance.Stop();
                    break;
                case CandidateType.NewRequest:
                    // Just ignore, it never gets born.
                    break;
            }
        }
        
        // Now that we have freed up slots, we can safely play the top 64.
        int playCount = Math.Min(_candidates.Count, MaxVoices);
        for (int i = 0; i < playCount; i++)
        {
            var candidate = _candidates[i];

            float pan = CalculatePan(candidate.WorldPosition, listenerPos);

            switch (candidate.Type)
            {
                case CandidateType.Emitter:
                    UpdateEmitter(candidate.EmitterEntity, candidate.Importance, pan);
                    break;
                case CandidateType.ActiveOneShot:
                    var shot = candidate.ActiveReference;
                    shot.Instance.Volume = MathHelper.Clamp(candidate.Importance, 0f, 1f);
                    shot.Instance.Pan = pan;
                    break;
                case CandidateType.NewRequest:
                    PlayNewRequest(candidate.RequestData, candidate.Importance, pan);
                    break;
            }
        }
    }
    
    private void UpdateEmitter(Entity entity, float targetVolume, float targetPan)
    {
        ref var component = ref entity.Get<ContinousSoundEmitter>();
        
        if (component.ActiveInstance == null || component.ActiveInstance.IsDisposed)
        {
            var sfx = GetSoundEffect(component.Name);
            if (sfx != null)
            {
                component.ActiveInstance = sfx.CreateInstance();
                component.ActiveInstance.IsLooped = component.Loop;
            }
            else
            {
                return;
            }
        }

        var instance = component.ActiveInstance;
        
        instance.Volume = MathHelper.Clamp(targetVolume, 0f, 1f);
        instance.Pitch = MathHelper.Clamp(component.Pitch, -1f, 1f);
        
        if (component.IsPositional)
            instance.Pan = MathHelper.Clamp(targetPan, -1f, 1f);
        else
            instance.Pan = 0f;
        
        if (instance.State != SoundState.Playing)
        {
            instance.Play();
        }
    }

    private void PlayNewRequest(PlaySoundRequest request, float volume, float pan)
    {
        var sfx = GetSoundEffect(request.SoundName);
        if (sfx == null) return;

        var instance = sfx.CreateInstance();
        
        float pitch = request.Pitch;
        if (request.PitchVariance > 0)
        {
            float random = (float)(_rng.NextDouble() * 2.0 - 1.0);
            pitch += random * request.PitchVariance;
        }

        instance.Volume = MathHelper.Clamp(volume, 0f, 1f);
        instance.Pitch = MathHelper.Clamp(pitch, -1f, 1f);
        instance.Pan = request.IsGlobal ? 0f : MathHelper.Clamp(pan, -1f, 1f);

        instance.Play();

        _activeOneShots.Add(new ActiveOneShot
        {
            Instance = instance,
            IsGlobal = request.IsGlobal,
            Position = request.WorldPosition,
            Range = request.Range,
            Priority = request.Priority,
            BaseVolume = request.Volume
        });
    }

    /// <summary>
    /// Calculates volume based on distance using a squared falloff curve.
    /// Squared sounds more natural than Linear.
    /// </summary>
    private float CalculateVolume(float baseVolume, float distance, float range, float masterVolume, bool isPositional)
    {
        if (!isPositional) return baseVolume * masterVolume;

        // Normalize distance (0.0 = on top of listener, 1.0 = at max range)
        float normalizedDist = MathHelper.Clamp(distance / range, 0f, 1f);
        
        // Invert it (1.0 = close, 0.0 = far)
        float inverseDist = 1.0f - normalizedDist;

        // Square it for natural falloff
        float distanceFactor = inverseDist * inverseDist;

        return baseVolume * distanceFactor * masterVolume;
    }
    
    private float CalculateImportance(float vol, int priority, bool isEmitter)
    {
        float emitterBonus = isEmitter ? 1.2f : 1f;
        
        return ((vol + priority * 0.3f) * emitterBonus);
    }

    /// <summary>
    /// Calculates stereo pan (-1.0 left to 1.0 right) based on X position difference.
    /// </summary>
    private float CalculatePan(Vector2 emitterPos, Vector2 listenerPos)
    {
        float xDiff = emitterPos.X - listenerPos.X;
        
        // Divide by the range to get a percentage (e.g., 500px diff / 1000px range = 0.5)
        float pan = xDiff / PanRange;
        
        return MathHelper.Clamp(pan, -1f, 1f);
    }

    public void PlaySound(PlaySoundRequest request)
    {
        _requestQueue.Enqueue(request);
    }

    public void ClearSoundQueue()
    {
        _requestQueue.Clear();
    }

    public void StopAllSounds()
    {
        // sfx
        foreach (var shot in _activeOneShots)
        {
            shot.Instance.Stop();
            shot.Instance.Dispose();
        }
        _activeOneShots.Clear();

        // emitters
        foreach (ref readonly var entity in _query.GetEntities())
        {
            ref var em = ref entity.Get<ContinousSoundEmitter>();
            if (em.ActiveInstance != null)
            {
                em.ActiveInstance.Stop();
            }
        }
    }
    
    private enum CandidateType { Emitter, NewRequest, ActiveOneShot }

    private struct VoiceCandidate
    {
        public CandidateType Type;
        public float Importance;
        public Vector2 WorldPosition;

        public Entity EmitterEntity;
        public PlaySoundRequest RequestData;
        public ActiveOneShot ActiveReference;
    }

    private struct ActiveOneShot
    {
        public SoundEffectInstance Instance;
        public Vector2 Position;
        public float Range;
        public bool IsGlobal;
        public int Priority;
        public float BaseVolume;
    }
}

public struct PlaySoundRequest()
{
    public string SoundName;
    public float Volume;
    public float Pitch;
    public float PitchVariance;
    public float Range;
    public int Priority;

    public bool IsGlobal = true;
    public Vector2 WorldPosition = new Vector2(0, 0);
}