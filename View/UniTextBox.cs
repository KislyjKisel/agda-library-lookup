using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using AgdaLibraryLookup.Model;

namespace AgdaLibraryLookup.View
{
    public class UniTextBox : TextBox, INotifyPropertyChanged
    {
        public UniTextBox()
        {
            _unicodeInput.PropertyChanged += (_, e) =>
            {
                string? propname = e.PropertyName switch
                {
                    nameof(_unicodeInput.IsActive) => nameof(this.UnicodeInputActive),
                    _ => null
                };
                this.PropertyChanged?.Invoke(this, new(propname));
            };
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (_disableUnicodeInput || e.Text.Length != 1) return;
            _unicodeInput.Process(Char.IsWhiteSpace(e.Text[0]) ? ' ' : e.Text[0])
                .Map(a => {
                    HandleUnicodeReplacement(a);
                    e.Handled = true;
                });

            base.OnPreviewTextInput(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Space || e.Key == Key.Return || e.Key == Key.Tab || e.Key == Key.Back || e.Key == Key.Delete)
            {
                _unicodeInput.Process(' ').Map(HandleUnicodeReplacement);
            }

            base.OnPreviewKeyDown(e);
        }

        private void HandleUnicodeReplacement((int, string) arg)
        {
            (int Offset, string Value) = arg;
            _disableUnicodeInput = true;
            int ci = this.CaretIndex;
            this.Text = this.Text[..(ci - Offset + 1)] + Value + (ci < this.Text.Length ? this.Text[ci..] : String.Empty);
            this.CaretIndex = ci + Value.Length - (Offset - 1);
            _disableUnicodeInput = false;
        }

        private bool _disableUnicodeInput = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool UnicodeInputActive => _unicodeInput.IsActive;

        private readonly UnicodeInput _unicodeInput = new();
    }
}
