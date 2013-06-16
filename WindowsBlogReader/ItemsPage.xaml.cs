using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Il modello di elemento per la pagina degli elementi è documentato all'indirizzo http://go.microsoft.com/fwlink/?LinkId=234233

namespace WindowsBlogReader
{
    /// <summary>
    /// Pagina in cui è visualizzata una raccolta di anteprime elementi. Nell'applicazione divisa, questa pagina
    /// viene utilizzata per visualizzare e selezionare uno dei gruppi disponibili.
    /// </summary>
    public sealed partial class ItemsPage : WindowsBlogReader.Common.LayoutAwarePage
    {
        public ItemsPage()
        {
            this.InitializeComponent();
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
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: assegnare una raccolta associabile di elementi a this.DefaultViewModel["Items"]
            FeedDataSource feedDataSource = (FeedDataSource)App.Current.Resources["feedDataSource"];

            if (feedDataSource != null)
            {
                this.DefaultViewModel["Items"] = feedDataSource.Feeds;
            }

        }

        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem != null)
            {
                string title = ((FeedData)e.ClickedItem).Title;
                this.Frame.Navigate(typeof(SplitPage), title);
            }
        }

        
    }
}
