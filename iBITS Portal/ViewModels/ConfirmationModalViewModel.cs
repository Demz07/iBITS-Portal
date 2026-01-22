namespace iBITS_Portal.ViewModels
{
    public class ConfirmationModalViewModel
    {
        public string ModalId { get; set; } = "confirmationModal";
        public string ModalTitle { get; set; } = "Confirm Action";
        public string ModalBody { get; set; } = "Are you sure you want to proceed?";
        public string ConfirmButtonText { get; set; } = "Confirm";
        public string ConfirmButtonClass { get; set; } = "btn btn-primary";
    }
}