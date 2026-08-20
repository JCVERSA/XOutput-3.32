using XOutput.UI;

namespace XOutput.UI.Shell.Overlays
{
    /// <summary>
    /// Model for the themed message overlay (MessageBox replacement).
    /// </summary>
    public sealed class MessageOverlayModel : ModelBase
    {
        private string title;
        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        private string message;
        public string Message
        {
            get => message;
            set
            {
                if (message != value)
                {
                    message = value;
                    OnPropertyChanged(nameof(Message));
                }
            }
        }
    }

    /// <summary>
    /// View model for the themed message overlay (MessageBox replacement).
    /// </summary>
    public sealed class MessageOverlayViewModel : ViewModelBase<MessageOverlayModel>
    {
        public string Title => Model.Title;
        public string Message => Model.Message;

        public MessageOverlayViewModel(MessageOverlayModel model, string title, string message) : base(model)
        {
            Model.Title = title;
            Model.Message = message;
        }
    }
}
