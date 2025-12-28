using Microsoft.Xna.Framework.Input;

namespace MagicEngine.Engine.ECS.Core.Input
{
    /// <summary>
    /// Abstract base class for any physical input that can be bound to an action.
    /// </summary>
    public abstract class InputBinding
    {
        /// <summary>
        /// Checks if this specific input is currently active (pressed/down).
        /// </summary>
        public abstract bool IsDown(KeyboardState keyboardState, MouseState mouseState);
    }

    /// <summary>
    /// Represents a specific keyboard key binding.
    /// </summary>
    public class KeyBinding : InputBinding
    {
        private readonly Keys _key;

        public KeyBinding(Keys key)
        {
            _key = key;
        }

        public override bool IsDown(KeyboardState keyboardState, MouseState mouseState)
        {
            return keyboardState.IsKeyDown(_key);
        }
    }
    
    public enum MouseButton { Left, Right, Middle }

    /// <summary>
    /// Represents a specific mouse button binding.
    /// </summary>
    public class MouseButtonBinding : InputBinding
    {
        private readonly MouseButton _button;

        public MouseButtonBinding(MouseButton button)
        {
            _button = button;
        }

        public override bool IsDown(KeyboardState keyboardState, MouseState mouseState)
        {
            switch (_button)
            {
                case MouseButton.Left:
                    return mouseState.LeftButton == ButtonState.Pressed;
                case MouseButton.Right:
                    return mouseState.RightButton == ButtonState.Pressed;
                case MouseButton.Middle:
                    return mouseState.MiddleButton == ButtonState.Pressed;
                default:
                    return false;
            }
        }
    }
}