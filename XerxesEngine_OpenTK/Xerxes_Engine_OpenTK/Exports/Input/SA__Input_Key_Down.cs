using OpenTK.Input;

namespace Xerxes.Xerxes_OpenTK.Exports.Input
{
    public sealed class SA__Input_Key_Down :
        SA__Input_Key
    {
        internal SA__Input_Key_Down
        (
            double elapsedTime, 
            double deltaTime, 
            KeyboardKeyEventArgs keyboardKeyEventArgs
        ) 
        : base
        (
            elapsedTime, 
            deltaTime, 
            keyboardKeyEventArgs
        )
        {
        }
    }
}
