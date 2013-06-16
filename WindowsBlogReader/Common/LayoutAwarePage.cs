using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace WindowsBlogReader.Common
{
    /// <summary>
    /// Implementazione tipica di Page che fornisce varie importanti funzionalità:
    /// <list type="bullet">
    /// <item>
    /// <description>Stato di visualizzazione dell'applicazione a mapping dello stato di visualizzazione</description>
    /// </item>
    /// <item>
    /// <description>Gestori eventi GoBack, GoForward e GoHome</description>
    /// </item>
    /// <item>
    /// <description>Tasti di scelta rapida di mouse e tastiera per la navigazione</description>
    /// </item>
    /// <item>
    /// <description>Gestione stato per la navigazione e gestione del ciclo di vita dei processi</description>
    /// </item>
    /// <item>
    /// <description>Modello di visualizzazione predefinito</description>
    /// </item>
    /// </list>
    /// </summary>
    [Windows.Foundation.Metadata.WebHostHidden]
    public class LayoutAwarePage : Page
    {
        /// <summary>
        /// Identifica la proprietà di dipendenza <see cref="DefaultViewModel"/>.
        /// </summary>
        public static readonly DependencyProperty DefaultViewModelProperty =
            DependencyProperty.Register("DefaultViewModel", typeof(IObservableMap<String, Object>),
            typeof(LayoutAwarePage), null);

        private List<Control> _layoutAwareControls;

        /// <summary>
        /// Inizializza una nuova istanza della classe <see cref="LayoutAwarePage"/>.
        /// </summary>
        public LayoutAwarePage()
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled) return;

            // Crea un modello di visualizzazione predefinito vuoto
            this.DefaultViewModel = new ObservableDictionary<String, Object>();

            // Quando questa pagina è parte della struttura ad albero visuale, effettua due modifiche:
            // 1) Esegui il mapping dello stato di visualizzazione dell'applicazione allo stato di visualizzazione per la pagina
            // 2) Gestisci le richieste di navigazione di mouse e tastiera
            this.Loaded += (sender, e) =>
            {
                this.StartLayoutUpdates(sender, e);

                // La navigazione mediante tastiera e mouse è applicabile solo quando la finestra viene occupata per intero
                if (this.ActualHeight == Window.Current.Bounds.Height &&
                    this.ActualWidth == Window.Current.Bounds.Width)
                {
                    // Ascolta la finestra direttamente, in modo che non ne sia richiesto lo stato attivo
                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated +=
                        CoreDispatcher_AcceleratorKeyActivated;
                    Window.Current.CoreWindow.PointerPressed +=
                        this.CoreWindow_PointerPressed;
                }
            };

            // Annulla le stesse modifiche quando la pagina non è più visibile
            this.Unloaded += (sender, e) =>
            {
                this.StopLayoutUpdates(sender, e);
                Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -=
                    CoreDispatcher_AcceleratorKeyActivated;
                Window.Current.CoreWindow.PointerPressed -=
                    this.CoreWindow_PointerPressed;
            };
        }

        /// <summary>
        /// Implementazione di <see cref="IObservableMap&lt;String, Object&gt;"/> progettata per essere
        /// utilizzata come semplice modello di visualizzazione.
        /// </summary>
        protected IObservableMap<String, Object> DefaultViewModel
        {
            get
            {
                return this.GetValue(DefaultViewModelProperty) as IObservableMap<String, Object>;
            }

            set
            {
                this.SetValue(DefaultViewModelProperty, value);
            }
        }

        #region Supporto per la navigazione

        /// <summary>
        /// Richiamato come gestore eventi per eseguire la navigazione a ritroso nel <see cref="Frame"/> associato alla pagina
        /// fino a raggiungere il livello principale dello stack di navigazione.
        /// </summary>
        /// <param name="sender">Istanza che ha generato l'evento.</param>
        /// <param name="e">Dati evento che descrivono le condizioni che hanno determinato l'evento.</param>
        protected virtual void GoHome(object sender, RoutedEventArgs e)
        {
            // Utilizzare il frame di navigazione per tornare alla pagina in primo piano
            if (this.Frame != null)
            {
                while (this.Frame.CanGoBack) this.Frame.GoBack();
            }
        }

        /// <summary>
        /// Richiamato come gestore eventi per eseguire la navigazione a ritroso nello stack di navigazione
        /// associato al <see cref="Frame"/> di questa pagina.
        /// </summary>
        /// <param name="sender">Istanza che ha generato l'evento.</param>
        /// <param name="e">Dati evento che descrivono le condizioni che hanno generato l'evento
        /// stesso.</param>
        protected virtual void GoBack(object sender, RoutedEventArgs e)
        {
            // Utilizzare il frame di navigazione per tornare alla pagina precedente
            if (this.Frame != null && this.Frame.CanGoBack) this.Frame.GoBack();
        }

        /// <summary>
        /// Richiamato come gestore eventi per navigare in avanti nello stack di navigazione
        /// associato al <see cref="Frame"/> di questa pagina.
        /// </summary>
        /// <param name="sender">Istanza che ha generato l'evento.</param>
        /// <param name="e">Dati evento che descrivono le condizioni che hanno generato l'evento
        /// stesso.</param>
        protected virtual void GoForward(object sender, RoutedEventArgs e)
        {
            // Utilizzare il frame di navigazione per passare alla pagina successiva
            if (this.Frame != null && this.Frame.CanGoForward) this.Frame.GoForward();
        }

        /// <summary>
        /// Richiamato per ciascuna sequenza di tasti, compresi i tasti di sistema quali combinazioni con il tasto ALT, quando
        /// questa pagina è attiva e occupa l'intera finestra. Utilizzato per il rilevamento della navigazione da tastiera
        /// tra pagine, anche quando la pagina stessa no dispone dello stato attivo.
        /// </summary>
        /// <param name="sender">Istanza che ha generato l'evento.</param>
        /// <param name="args">Dati evento che descrivono le condizioni che hanno determinato l'evento.</param>
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender,
            AcceleratorKeyEventArgs args)
        {
            var virtualKey = args.VirtualKey;

            // Esegui ulteriori controlli solo se vengono premuti i tasti Freccia SINISTRA, Freccia DESTRA o i tasti dedicati Precedente
            // o successivo
            if ((args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown ||
                args.EventType == CoreAcceleratorKeyEventType.KeyDown) &&
                (virtualKey == VirtualKey.Left || virtualKey == VirtualKey.Right ||
                (int)virtualKey == 166 || (int)virtualKey == 167))
            {
                var coreWindow = Window.Current.CoreWindow;
                var downState = CoreVirtualKeyStates.Down;
                bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
                bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
                bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
                bool noModifiers = !menuKey && !controlKey && !shiftKey;
                bool onlyAlt = menuKey && !controlKey && !shiftKey;

                if (((int)virtualKey == 166 && noModifiers) ||
                    (virtualKey == VirtualKey.Left && onlyAlt))
                {
                    // Quando viene premuto il tasto Precedente o ALT+Freccia SINISTRA, torna indietro
                    args.Handled = true;
                    this.GoBack(this, new RoutedEventArgs());
                }
                else if (((int)virtualKey == 167 && noModifiers) ||
                    (virtualKey == VirtualKey.Right && onlyAlt))
                {
                    // Quando viene premuto il tasto Successivo o ALT+Freccia DESTRA, vai avanti
                    args.Handled = true;
                    this.GoForward(this, new RoutedEventArgs());
                }
            }
        }

        /// <summary>
        /// Richiamato per ciascun clic del mouse, tocco del touch screen o interazione equivalente quando la
        /// pagina è attiva e occupa per intero la finestra. Utilizzato per il rilevamento del clic del mouse sui pulsanti di tipo browser
        /// Precedente e Successivo per navigare tra pagine.
        /// </summary>
        /// <param name="sender">Istanza che ha generato l'evento.</param>
        /// <param name="args">Dati evento che descrivono le condizioni che hanno determinato l'evento.</param>
        private void CoreWindow_PointerPressed(CoreWindow sender,
            PointerEventArgs args)
        {
            var properties = args.CurrentPoint.Properties;

            // Ignora combinazioni di pulsanti con i pulsanti sinistro destro e centrale
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed ||
                properties.IsMiddleButtonPressed) return;

            // Se viene premuto Precedente o Successivo (ma non entrambi) naviga come appropriato
            bool backPressed = properties.IsXButton1Pressed;
            bool forwardPressed = properties.IsXButton2Pressed;
            if (backPressed ^ forwardPressed)
            {
                args.Handled = true;
                if (backPressed) this.GoBack(this, new RoutedEventArgs());
                if (forwardPressed) this.GoForward(this, new RoutedEventArgs());
            }
        }

        #endregion

        #region Commutazione stato di visualizzazione

        /// <summary>
        /// Richiamato come gestore eventi, in genere sull'evento <see cref="FrameworkElement.Loaded"/>
        /// di un <see cref="Control"/> all'interno della pagina, per indicare che il mittente
        /// deve iniziare a ricevere le modifiche della gestione dello stato di visualizzazione corrispondenti alle modifiche di stato
        /// di visualizzazione dell'applicazione.
        /// </summary>
        /// <param name="sender">Istanza di <see cref="Control"/> che supporta la gestione dello stato
        /// di visualizzazione corrispondente agli stati di visualizzazione.</param>
        /// <param name="e">Dati dell'evento in cui viene descritto in che modo è stata effettuata la richiesta.</param>
        /// <remarks>Lo stato di visualizzazione corrente verrà utilizzato immediatamente per impostare lo stato di visualizzazione
        /// corrispondente quando vengono richiesti aggiornamenti del layout. Un gestore eventi
        /// <see cref="FrameworkElement.Unloaded"/> connesso a
        /// <see cref="StopLayoutUpdates"/> è vivamente consigliato. Le istanze di
        /// <see cref="LayoutAwarePage"/> richiamano automaticamente questi gestori nei propri eventi Loaded e
        /// Unloaded.</remarks>
        /// <seealso cref="DetermineVisualState"/>
        /// <seealso cref="InvalidateVisualState"/>
        public void StartLayoutUpdates(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            if (control == null) return;
            if (this._layoutAwareControls == null)
            {
                // Avvia l'ascolto delle modifiche dello stato di visualizzazione quando vi sono controlli interessati dagli aggiornamenti
                Window.Current.SizeChanged += this.WindowSizeChanged;
                this._layoutAwareControls = new List<Control>();
            }
            this._layoutAwareControls.Add(control);

            // Imposta lo stato di visualizzazione iniziale del controllo
            VisualStateManager.GoToState(control, DetermineVisualState(ApplicationView.Value), false);
        }

        private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            this.InvalidateVisualState();
        }

        /// <summary>
        /// Richiamato come gestore eventi, in genere sull'evento <see cref="FrameworkElement.Unloaded"/>
        /// di un <see cref="Control"/>, per indicare che il mittente deve iniziare a ricevere
        /// le modifiche dello stato di visualizzazione corrispondenti alle modifiche dello stato di visualizzazione dell'applicazione.
        /// </summary>
        /// <param name="sender">Istanza di <see cref="Control"/> che supporta la gestione dello stato
        /// di visualizzazione corrispondente agli stati di visualizzazione.</param>
        /// <param name="e">Dati dell'evento in cui viene descritto in che modo è stata effettuata la richiesta.</param>
        /// <remarks>Lo stato di visualizzazione corrente verrà utilizzato immediatamente per impostare lo stato di visualizzazione
        /// lo stato di visualizzazione quando vengono richiesti aggiornamenti del layout.</remarks>
        /// <seealso cref="StartLayoutUpdates"/>
        public void StopLayoutUpdates(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            if (control == null || this._layoutAwareControls == null) return;
            this._layoutAwareControls.Remove(control);
            if (this._layoutAwareControls.Count == 0)
            {
                // Arresta l'ascolto delle modifiche dello stato di visualizzazione quando non vi sono controlli interessati dagli aggiornamenti
                this._layoutAwareControls = null;
                Window.Current.SizeChanged -= this.WindowSizeChanged;
            }
        }

        /// <summary>
        /// Traduce valori <see cref="ApplicationViewState"/> in stringhe per la gestione
        /// dello stato di visualizzazione all'interno della pagina. Nell'implementazione predefinita vengono utilizzati i nomi dei valori enum.
        /// Le sottoclassi possono eseguire l'override di questo metodo per controllare lo schema di mapping utilizzato.
        /// </summary>
        /// <param name="viewState">Stato di visualizzazione per il quale è richiesto un valore di stato.</param>
        /// <returns>Nome dello stato di visualizzazione utilizzato per eseguire
        /// <see cref="VisualStateManager"/></returns>
        /// <seealso cref="InvalidateVisualState"/>
        protected virtual string DetermineVisualState(ApplicationViewState viewState)
        {
            return viewState.ToString();
        }

        /// <summary>
        /// Aggiorna tutti i controlli in attesa di modifiche dello stato di visualizzazione con il corretto
        /// stato di visualizzazione.
        /// </summary>
        /// <remarks>
        /// Normalmente utilizzato in combinazione con l'override <see cref="DetermineVisualState"/> per
        /// segnalare che potrebbe essere restituito un valore diverso anche qualora lo stato di visualizzazione non sia stato
        /// modificato.
        /// </remarks>
        public void InvalidateVisualState()
        {
            if (this._layoutAwareControls != null)
            {
                string visualState = DetermineVisualState(ApplicationView.Value);
                foreach (var layoutAwareControl in this._layoutAwareControls)
                {
                    VisualStateManager.GoToState(layoutAwareControl, visualState, false);
                }
            }
        }

        #endregion

        #region Gestione del ciclo di vita dei processi

        private String _pageKey;

        /// <summary>
        /// Richiamato quando la pagina sta per essere visualizzata in un Frame.
        /// </summary>
        /// <param name="e">Dati dell'evento in cui vengono descritte le modalità con cui la pagina è stata raggiunta. La proprietà
        /// Parameter fornisce il gruppo da visualizzare.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Il ritorno a una pagina memorizzata nella cache tramite la navigazione non deve attivare il caricamento dello stato
            if (this._pageKey != null) return;

            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            this._pageKey = "Page-" + this.Frame.BackStackDepth;

            if (e.NavigationMode == NavigationMode.New)
            {
                // Cancella lo stato esistente per la navigazione in avanti quando si aggiunge una nuova pagina allo
                // stack di navigazione
                var nextPageKey = this._pageKey;
                int nextPageIndex = this.Frame.BackStackDepth;
                while (frameState.Remove(nextPageKey))
                {
                    nextPageIndex++;
                    nextPageKey = "Page-" + nextPageIndex;
                }

                // Passa il parametro di navigazione alla nuova pagina
                this.LoadState(e.Parameter, null);
            }
            else
            {
                // Passa il parametro di navigazione e lo stato della pagina mantenuto, utilizzando
                // la stessa strategia per caricare lo stato sospeso e ricreare le pagine scartate
                // dalla cache
                this.LoadState(e.Parameter, (Dictionary<String, Object>)frameState[this._pageKey]);
            }
        }

        /// <summary>
        /// Richiamato quando questa pagina non verrà più visualizzata in un frame.
        /// </summary>
        /// <param name="e">Dati dell'evento in cui vengono descritte le modalità con cui la pagina è stata raggiunta. La proprietà
        /// Parameter fornisce il gruppo da visualizzare.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            var pageState = new Dictionary<String, Object>();
            this.SaveState(pageState);
            frameState[_pageKey] = pageState;
        }

        /// <summary>
        /// Popola la pagina con il contenuto passato durante la navigazione. Vengono inoltre forniti eventuali stati
        /// salvati durante la ricreazione di una pagina in una sessione precedente.
        /// </summary>
        /// <param name="navigationParameter">Valore del parametro passato a
        /// <see cref="Frame.Navigate(Type, Object)"/> quando la pagina è stata inizialmente richiesta.
        /// </param>
        /// <param name="pageState">Dizionario di stato mantenuto da questa pagina nel corso di una sessione
        /// precedente. Il valore è null la prima volta che viene visitata una pagina.</param>
        protected virtual void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Mantiene lo stato associato a questa pagina in caso di sospensione dell'applicazione o se la
        /// viene scartata dalla cache di navigazione. I valori devono essere conformi ai requisiti di
        /// serializzazione di <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">Dizionario vuoto da popolare con uno stato serializzabile.</param>
        protected virtual void SaveState(Dictionary<String, Object> pageState)
        {
        }

        #endregion

        /// <summary>
        /// Implementazione di IObservableMap che supporta la reentrancy per l'utilizzo come modello di visualizzazione
        /// predefinito.
        /// </summary>
        private class ObservableDictionary<K, V> : IObservableMap<K, V>
        {
            private class ObservableDictionaryChangedEventArgs : IMapChangedEventArgs<K>
            {
                public ObservableDictionaryChangedEventArgs(CollectionChange change, K key)
                {
                    this.CollectionChange = change;
                    this.Key = key;
                }

                public CollectionChange CollectionChange { get; private set; }
                public K Key { get; private set; }
            }

            private Dictionary<K, V> _dictionary = new Dictionary<K, V>();
            public event MapChangedEventHandler<K, V> MapChanged;

            private void InvokeMapChanged(CollectionChange change, K key)
            {
                var eventHandler = MapChanged;
                if (eventHandler != null)
                {
                    eventHandler(this, new ObservableDictionaryChangedEventArgs(change, key));
                }
            }

            public void Add(K key, V value)
            {
                this._dictionary.Add(key, value);
                this.InvokeMapChanged(CollectionChange.ItemInserted, key);
            }

            public void Add(KeyValuePair<K, V> item)
            {
                this.Add(item.Key, item.Value);
            }

            public bool Remove(K key)
            {
                if (this._dictionary.Remove(key))
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                    return true;
                }
                return false;
            }

            public bool Remove(KeyValuePair<K, V> item)
            {
                V currentValue;
                if (this._dictionary.TryGetValue(item.Key, out currentValue) &&
                    Object.Equals(item.Value, currentValue) && this._dictionary.Remove(item.Key))
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, item.Key);
                    return true;
                }
                return false;
            }

            public V this[K key]
            {
                get
                {
                    return this._dictionary[key];
                }
                set
                {
                    this._dictionary[key] = value;
                    this.InvokeMapChanged(CollectionChange.ItemChanged, key);
                }
            }

            public void Clear()
            {
                var priorKeys = this._dictionary.Keys.ToArray();
                this._dictionary.Clear();
                foreach (var key in priorKeys)
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                }
            }

            public ICollection<K> Keys
            {
                get { return this._dictionary.Keys; }
            }

            public bool ContainsKey(K key)
            {
                return this._dictionary.ContainsKey(key);
            }

            public bool TryGetValue(K key, out V value)
            {
                return this._dictionary.TryGetValue(key, out value);
            }

            public ICollection<V> Values
            {
                get { return this._dictionary.Values; }
            }

            public bool Contains(KeyValuePair<K, V> item)
            {
                return this._dictionary.Contains(item);
            }

            public int Count
            {
                get { return this._dictionary.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
            {
                return this._dictionary.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this._dictionary.GetEnumerator();
            }

            public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
            {
                int arraySize = array.Length;
                foreach (var pair in this._dictionary)
                {
                    if (arrayIndex >= arraySize) break;
                    array[arrayIndex++] = pair;
                }
            }
        }
    }
}
