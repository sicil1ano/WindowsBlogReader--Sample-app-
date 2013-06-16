using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;

namespace WindowsBlogReader.Common
{
    /// <summary>
    /// Wrapper per <see cref="RichTextBlock"/> che crea tante colonne di overflow
    /// aggiuntive quante ne sono necessarie per ospitare il contenuto disponibile.
    /// </summary>
    /// <example>
    /// Nell'esempio seguente viene creata una raccolta di colonne della larghezza di 400 pixel con spaziatura di 50 pixel tra colonne
    /// per ospitare contenuto arbitrario con associazione dati:
    /// <code>
    /// <RichTextColumns>
    ///     <RichTextColumns.ColumnTemplate>
    ///         <DataTemplate>
    ///             <RichTextBlockOverflow Width="400" Margin="50,0,0,0"/>
    ///         </DataTemplate>
    ///     </RichTextColumns.ColumnTemplate>
    ///     
    ///     <RichTextBlock Width="400">
    ///         <Paragraph>
    ///             <Run Text="{Binding Content}"/>
    ///         </Paragraph>
    ///     </RichTextBlock>
    /// </RichTextColumns>
    /// </code>
    /// </example>
    /// <remarks>Normalmente utilizzato in una regione di scorrimento orizzontale in cui una quantità non limitata di
    /// spazio consente la creazione di tutte le colonne necessarie. Quando utilizzato in uno spazio di scorrimento
    /// verticale, non saranno mai presenti colonne aggiuntive.</remarks>
    [Windows.UI.Xaml.Markup.ContentProperty(Name = "RichTextContent")]
    public sealed class RichTextColumns : Panel
    {
        /// <summary>
        /// Identifica la proprietà di dipendenza <see cref="RichTextContent"/>.
        /// </summary>
        public static readonly DependencyProperty RichTextContentProperty =
            DependencyProperty.Register("RichTextContent", typeof(RichTextBlock),
            typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

        /// <summary>
        /// Identifica la proprietà di dipendenza <see cref="ColumnTemplate"/>.
        /// </summary>
        public static readonly DependencyProperty ColumnTemplateProperty =
            DependencyProperty.Register("ColumnTemplate", typeof(DataTemplate),
            typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

        /// <summary>
        /// Inizializza una nuova istanza della classe <see cref="RichTextColumns"/>.
        /// </summary>
        public RichTextColumns()
        {
            this.HorizontalAlignment = HorizontalAlignment.Left;
        }

        /// <summary>
        /// Ottiene o imposta il contenuto RTF originale da utilizzare come prima colonna.
        /// </summary>
        public RichTextBlock RichTextContent
        {
            get { return (RichTextBlock)GetValue(RichTextContentProperty); }
            set { SetValue(RichTextContentProperty, value); }
        }

        /// <summary>
        /// Ottiene o imposta il modello utilizzato per creare istanze
        /// <see cref="RichTextBlockOverflow"/> aggiuntive.
        /// </summary>
        public DataTemplate ColumnTemplate
        {
            get { return (DataTemplate)GetValue(ColumnTemplateProperty); }
            set { SetValue(ColumnTemplateProperty, value); }
        }

        /// <summary>
        /// Richiamato quando il modello di contenuto o di overflow viene modificato per ricreare il layout colonne.
        /// </summary>
        /// <param name="d">Istanza di <see cref="RichTextColumns"/> quando la modifica
        /// ha avuto luogo.</param>
        /// <param name="e">Dati evento che descrivono la modifica specifica.</param>
        private static void ResetOverflowLayout(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Se hanno luogo modifiche rilevanti, ricrea il layout da zero
            var target = d as RichTextColumns;
            if (target != null)
            {
                target._overflowColumns = null;
                target.Children.Clear();
                target.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Elenca le colonne di overflow già create.  Deve mantenere una relazione 1:1 con
        /// le istanze nella raccolta <see cref="Panel.Children"/> che seguono l'elemento figlio
        /// RichTextBlock iniziale.
        /// </summary>
        private List<RichTextBlockOverflow> _overflowColumns = null;

        /// <summary>
        /// Determina se sono necessarie colonne di overflow aggiuntive e se le colonne esistenti possono
        /// essere rimosse.
        /// </summary>
        /// <param name="availableSize">Dimensioni dello spazio disponibile, utilizzate per limitare il
        /// numero di colonne aggiuntive che può essere creato.</param>
        /// <returns>Le dimensioni risultanti del contenuto originale più eventuali colonne aggiuntive.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.RichTextContent == null) return new Size(0, 0);

            // Assicurarsi che RichTextBlock sia un elemento figlio, utilizzando l'assenza di
            // un elenco di colonne aggiuntive come indicazione che non è ancora
            // stato creato
            if (this._overflowColumns == null)
            {
                Children.Add(this.RichTextContent);
                this._overflowColumns = new List<RichTextBlockOverflow>();
            }

            // Iniziare misurando il contenuto dell'elemento RichTextBlock originale
            this.RichTextContent.Measure(availableSize);
            var maxWidth = this.RichTextContent.DesiredSize.Width;
            var maxHeight = this.RichTextContent.DesiredSize.Height;
            var hasOverflow = this.RichTextContent.HasOverflowContent;

            // Assicurarsi che siamo presenti sufficienti colonne di overflow
            int overflowIndex = 0;
            while (hasOverflow && maxWidth < availableSize.Width && this.ColumnTemplate != null)
            {
                // Utilizzare le colonne di overflow esistenti fino al loro esaurimento, quindi crearne
                // altre dal modello fornito
                RichTextBlockOverflow overflow;
                if (this._overflowColumns.Count > overflowIndex)
                {
                    overflow = this._overflowColumns[overflowIndex];
                }
                else
                {
                    overflow = (RichTextBlockOverflow)this.ColumnTemplate.LoadContent();
                    this._overflowColumns.Add(overflow);
                    this.Children.Add(overflow);
                    if (overflowIndex == 0)
                    {
                        this.RichTextContent.OverflowContentTarget = overflow;
                    }
                    else
                    {
                        this._overflowColumns[overflowIndex - 1].OverflowContentTarget = overflow;
                    }
                }

                // Misurare la nuova colonna e prepararsi a ripetere l'operazione, se necessario
                overflow.Measure(new Size(availableSize.Width - maxWidth, availableSize.Height));
                maxWidth += overflow.DesiredSize.Width;
                maxHeight = Math.Max(maxHeight, overflow.DesiredSize.Height);
                hasOverflow = overflow.HasOverflowContent;
                overflowIndex++;
            }

            // Disconnettere le colonne aggiuntive dalla catena di overflow, rimuoverle dall'elenco privato
            // di colonne, quindi rimuoverle come elementi figlio
            if (this._overflowColumns.Count > overflowIndex)
            {
                if (overflowIndex == 0)
                {
                    this.RichTextContent.OverflowContentTarget = null;
                }
                else
                {
                    this._overflowColumns[overflowIndex - 1].OverflowContentTarget = null;
                }
                while (this._overflowColumns.Count > overflowIndex)
                {
                    this._overflowColumns.RemoveAt(overflowIndex);
                    this.Children.RemoveAt(overflowIndex + 1);
                }
            }

            // Indicare le dimensioni finali determinate
            return new Size(maxWidth, maxHeight);
        }

        /// <summary>
        /// Dispone il contenuto originale e tutte le colonne aggiuntive.
        /// </summary>
        /// <param name="finalSize">Definisce le dimensioni dell'area entro cui devono essere disposti gli elementi
        /// figlio.</param>
        /// <returns>Dimensioni dell'area effettivamente richiesta dagli elementi figlio.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double maxWidth = 0;
            double maxHeight = 0;
            foreach (var child in Children)
            {
                child.Arrange(new Rect(maxWidth, 0, child.DesiredSize.Width, finalSize.Height));
                maxWidth += child.DesiredSize.Width;
                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }
            return new Size(maxWidth, maxHeight);
        }
    }
}
