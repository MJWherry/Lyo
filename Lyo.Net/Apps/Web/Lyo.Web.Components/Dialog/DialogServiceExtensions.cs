namespace Lyo.Web.Components.Dialog;

/// <summary>Confirmation prompts with consistent wording and button labels, so a destructive action reads the same everywhere in the app.</summary>
public static class DialogServiceExtensions
{
    extension(IDialogService dialogService)
    {
        /// <summary>
        /// Asks the user to confirm deleting <paramref name="subject" />. Returns true only on an explicit confirm; dismissing the dialog counts as cancel.
        /// </summary>
        /// <param name="subject">What is being deleted, as it should read in the prompt, for example <c>definition 'Nightly export'</c>.</param>
        /// <param name="consequences">Extra sentence spelling out side effects, such as cascading deletes. Omit when there are none.</param>
        /// <param name="title">Dialog title. Default is "Delete".</param>
        /// <param name="confirmText">Confirm button label. Default is "Delete".</param>
        public async Task<bool> ConfirmDeleteAsync(string subject, string? consequences = null, string title = "Delete", string confirmText = "Delete")
        {
            var message = string.IsNullOrWhiteSpace(consequences) ? $"Delete {subject}?" : $"Delete {subject}? {consequences}";
            return await dialogService.ShowMessageBoxAsync(title, message, confirmText, cancelText: "Cancel") == true;
        }

        /// <summary>Asks the user to confirm a non-delete action that cannot be undone, such as purging a queue or reverting a revision.</summary>
        /// <param name="title">Dialog title naming the action.</param>
        /// <param name="message">Full prompt text.</param>
        /// <param name="confirmText">Confirm button label.</param>
        public async Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm")
            => await dialogService.ShowMessageBoxAsync(title, message, confirmText, cancelText: "Cancel") == true;
    }
}
