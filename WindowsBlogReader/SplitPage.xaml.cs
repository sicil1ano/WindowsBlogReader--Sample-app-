using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Il modello di elemento per la pagina divisa è documentato all'indirizzo http://go.microsoft.com/fwlink/?LinkId=234234

namespace WindowsBlogReader
{
    /// <summary>
    /// Pagina in cui viene visualizzato un titolo gruppo, un elenco di elementi all'interno del gruppo e i dettagli relativi
    /// all'elemento correntemente selezionato.
    /// </summary>
    public sealed partial class SplitPage : WindowsBlogReader.Common.LayoutAwarePage
    {
        public SplitPage()
        {
            this.InitializeComponent();
        }

        #region Gestione dello stato della pagina

        /// <summary>
        /// Popola la pagina con il contenuto passato durante la navigazione. Vengono inoltre forniti eventuali stati
        /// salvati durante la ricreazione di una pagina in una sessione precedente.
        /// </summary>
        /// <param name="navigationParameter">Valore del parametro passato a
        /// <see cref="Frame.Navigate(Type, Object)"/> quando la pagina è stata inizialmente richiesta.
        /// </param>
        /// <param name="pageState">Dizionario di stato mantenuto da questa pagina nel corso di una sessione
        /// precedente. Il valore è null la prima volta che viene visitata una pagina.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: assegnare un gruppo associabile a this.DefaultViewModel["Gruppo"]
            // TODO: assegnare una raccolta di elementi associabili a this.DefaultViewModel["Elementi"]
            string feedTitle = (string)navigationParameter;
            FeedData feedData = FeedDataSource.GetFeed(feedTitle);
            if (feedData != null)
            {
                this.DefaultViewModel["Feed"] = feedData;
                this.DefaultViewModel["Items"] = feedData.Items;
            }

            if (pageState == null)
            {
                // When this is a new page, select the first item automatically unless logical page
                // navigation is being used (see the logical page navigation #region below.)
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null)
                {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                }
                else
                {
                    this.itemsViewSource.View.MoveCurrentToPosition(-1);
                }
            }
            else
            {
                // Ripristinare lo stato salvato in precedenza con questa pagina
                if (pageState.ContainsKey("SelectedItem") && this.itemsViewSource.View != null)
                {
                    // TODO: richiamare this.itemsViewSource.View.MoveCurrentTo() con l'elemento
                    //       selezionato come specificato dal valore di pageState["SelectedItem"]
                    string itemTitle = (string)pageState["SelectedItem"];
                    FeedItem selectedItem = FeedDataSource.GetItem(itemTitle);
                    this.itemsViewSource.View.MoveCurrentTo(selectedItem);
                }
            }
        }

        /// <summary>
        /// Mantiene lo stato associato a questa pagina in caso di sospensione dell'applicazione o se la
        /// viene scartata dalla cache di navigazione. I valori devono essere conformi ai requisiti di
        /// serializzazione di <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">Dizionario vuoto da popolare con uno stato serializzabile.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            if (this.itemsViewSource.View != null)
            {
                var selectedItem = this.itemsViewSource.View.CurrentItem;
                // TODO: derivare un parametro di navigazione serializzabile e assegnarlo a
                //       pageState["SelectedItem"]
                if (selectedItem != null)
                {
                    string itemTitle = ((FeedItem)selectedItem).Title;
                    pageState["SelectedItem"] = itemTitle;
                }
            }
        }

        #endregion

        #region Navigazione all'interno di pagine logiche

        // La gestione dello stato di visualizzazione in genere rispecchia direttamente i quattro stati di visualizzazione dell'applicazione
        // (visualizzazioni Landscape e Portrait a schermo intero più le visualizzazioni Snapped e Filled). La pagina divisa è
        // progettata in modo che gli stati di visualizzazione Snapped e Portrait dispongano ognuno di due sottostati distinti:
        // viene visualizzato solo l'elenco di elementi oppure solo i dettagli ma non entrambi allo stesso tempo.
        //
        // Ciò è interamente implementato mediante una singola pagina fisica che può rappresentare due pagine
        // logiche. Nel codice seguente viene raggiunto questo obiettivo senza che l'utente si renda conto della
        // distinzione.

        /// <summary>
        /// Richiamato per determinare se la pagina deve funzionare come una singola pagina logica o come due pagine.
        /// </summary>
        /// <param name="viewState">Stato di visualizzazione in merito a cui viene posta la domanda, oppure null
        /// per lo stato di visualizzazione corrente. Questo parametro è facoltativo con il valore null come valore
        /// predefinito.</param>
        /// <returns>True quando lo stato di visualizzazione in questione è Portrait o Snapped, false
        /// in caso contrario.</returns>
        private bool UsingLogicalPageNavigation(ApplicationViewState? viewState = null)
        {
            if (viewState == null) viewState = ApplicationView.Value;
            return viewState == ApplicationViewState.FullScreenPortrait ||
                viewState == ApplicationViewState.Snapped;
        }

        /// <summary>
        /// Richiamato quando un elemento all'interno dell'elenco è selezionato.
        /// </summary>
        /// <param name="sender">Oggetto GridView (o ListView quando l'applicazione è nello stato Snapped)
        /// tramite cui viene visualizzato l'elemento selezionato.</param>
        /// <param name="e">Dati dell'evento in cui è descritto in che modo è stata modificata la selezione.</param>
        void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Invalida lo stato di visualizzazione quando è attiva la navigazione all'interno di pagine logiche, poiché una modifica
            // della selezione potrebbe dar luogo a una modifica corrispondente nella pagina logica corrente. Quando
            // un elemento è selezionato, l'operazione consente di passare dalla visualizzazione dell'elenco di elementi
            // a quella dei relativi dettagli. Quando la selezione viene annullata, l'operazione sortisce
            // l'effetto contrario.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();
            // Add this code to populate the web view
            //  with the content of the selected blog post.
            Selector list = sender as Selector;
            FeedItem selectedItem = list.SelectedItem as FeedItem;
            if (selectedItem != null)
            {
                this.contentView.NavigateToString(selectedItem.Content);
            }
            else
            {
                this.contentView.NavigateToString("");
            }      
        }

        /// <summary>
        /// Richiamato quando viene premuto il pulsante Indietro della pagina.
        /// </summary>
        /// <param name="sender">Istanza del pulsante Indietro.</param>
        /// <param name="e">Dati dell'evento in cui è descritto in che modo è stato fatto clic sul pulsante Indietro.</param>
        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            if (this.UsingLogicalPageNavigation() && itemListView.SelectedItem != null)
            {
                // Quando è attiva la navigazione all'interno di pagine logiche e vi è un elemento selezionato, vengono
                // visualizzati i dettagli di tale elemento. La cancellazione della selezione comporterà il ritorno
                // all'elenco di elementi. Dal punto di vista dell'utente, si tratta di un'operazione di navigazione logica
                // a ritroso.
                this.itemListView.SelectedItem = null;
            }
            else
            {
                // Quando la navigazione all'interno di pagine logiche non è attiva, o non vi è alcun elemento
                // selezionato, utilizza il comportamento predefinito del pulsante Indietro.
                base.GoBack(sender, e);
            }
        }

        /// <summary>
        /// Richiamato per determinare lo stato di visualizzazione corrispondente a quello di
        /// un'applicazione.
        /// </summary>
        /// <param name="viewState">Stato di visualizzazione in merito a cui viene posta la domanda.</param>
        /// <returns>Nome dello stato di visualizzazione desiderato. È lo stesso nome utilizzato per lo
        /// stato di visualizzazione, eccetto quando un elemento è selezionato nelle visualizzazioni Portrait e Snapped, nei cui casi
        /// questa pagina logica aggiuntiva viene rappresentata aggiungendo un suffisso _Detail.</returns>
        protected override string DetermineVisualState(ApplicationViewState viewState)
        {
            // Aggiorna lo stato abilitato del pulsante Indietro quando viene modificato lo stato di visualizzazione
            var logicalPageBack = this.UsingLogicalPageNavigation(viewState) && this.itemListView.SelectedItem != null;
            var physicalPageBack = this.Frame != null && this.Frame.CanGoBack;
            this.DefaultViewModel["CanGoBack"] = logicalPageBack || physicalPageBack;

            // Determinare gli stati di visualizzazione per i layout orizzontali basati non sullo stato di visualizzazione, ma
            // sulla larghezza della finestra. Questa pagina presenta un layout appropriato per
            // almeno 1366 pixel virtuali e un altro per display più stretti o quando un'applicazione ancorata
            // riduce lo spazio orizzontale disponibile a meno di 1366.
            if (viewState == ApplicationViewState.Filled ||
                viewState == ApplicationViewState.FullScreenLandscape)
            {
                var windowWidth = Window.Current.Bounds.Width;
                if (windowWidth >= 1366) return "FullScreenLandscapeOrWide";
                return "FilledOrNarrow";
            }

            // In caso di layout verticale o snapped iniziare con il nome di uno stato di visualizzazione predefinito, quindi aggiungere un
            // suffisso per la visualizzazione dei dettagli anziché l'elenco
            var defaultStateName = base.DetermineVisualState(viewState);
            return logicalPageBack ? defaultStateName + "_Detail" : defaultStateName;
        }

        #endregion

        private void ContentView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            string errorString = "<p>Page could not be loaded.</p><p>Error is : " + e.WebErrorStatus.ToString() + "</p>";
            this.contentView.NavigateToString(errorString);
        }

        private void ViewDetail_Click(object sender, RoutedEventArgs e)
        {
            FeedItem selectedItem = this.itemListView.SelectedItem as FeedItem;
            if (selectedItem != null && this.Frame != null)
            {
                string itemTitle = selectedItem.Title;
                this.Frame.Navigate(typeof(DetailPage), itemTitle);
            }
        }
    }
}
