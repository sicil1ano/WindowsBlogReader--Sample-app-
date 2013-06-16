using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Data;

namespace WindowsBlogReader.Common
{
    /// <summary>
    /// Implementazione di <see cref="INotifyPropertyChanged"/> per semplificare i modelli.
    /// </summary>
    [Windows.Foundation.Metadata.WebHostHidden]
    public abstract class BindableBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Evento multicast per le notifiche di modifica delle proprietà.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Verifica se una proprietà corrisponde già a un valore desiderato. Imposta la proprietà e
        /// invia le notifiche ai listener quando necessario.
        /// </summary>
        /// <typeparam name="T">Tipo della proprietà.</typeparam>
        /// <param name="storage">Riferimento a una proprietà dotata sia di getter che di setter.</param>
        /// <param name="value">Valore desiderato per la proprietà.</param>
        /// <param name="propertyName">Nome della proprietà utilizzato per la notifica ai listener. Si tratta
        /// di un valore facoltativo e può essere fornito automaticamente quando richiamato da compilatori che
        /// supportano CallerMemberName.</param>
        /// <returns>True se il valore è stato modificato, false se il valore esistente corrisponde al
        /// valore desiderato.</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Notifica ai listener che il valore di una proprietà è stato modificato.
        /// </summary>
        /// <param name="propertyName">Nome della proprietà utilizzato per la notifica ai listener. Si tratta
        /// di un valore facoltativo e può essere fornito automaticamente quando richiamato da compilatori
        /// che supportano <see cref="CallerMemberNameAttribute"/>.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
