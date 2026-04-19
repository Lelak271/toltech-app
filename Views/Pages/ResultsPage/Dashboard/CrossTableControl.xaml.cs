using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using TOLTECH_APPLICATION.Views;
using TOLTECH_APPLICATION.Services;
using TOLTECH_APPLICATION.ToltechCalculation.Resux;
using System.Windows.Data;

namespace TOLTECH_APPLICATION.FrontEnd.Controls.Dashboard
{
    /// <summary>
    /// Logique d'interaction pour CrossTableControl.xaml
    /// </summary>
    public partial class CrossTableControl : UserControl
    {
        private ResuxSerializer _resuxSerializer;
        public CrossTableControl()
        {
            InitializeComponent();
            _resuxSerializer = new ResuxSerializer();

            // Initialiser la ComboBox de sélection des vues
            var items = Enum.GetValues(typeof(CrossTableVariant))
                            .Cast<CrossTableVariant>()
                            .Select(v => new { Key = v, Value = GetCrossTableVariantDisplayName(v) })
                            .ToList();

            CbCrossTableVariant.ItemsSource = items;
            CbCrossTableVariant.SelectedValue = CrossTableVariant.InfluenceByContact;

            UpdateChartVisibility();

            ModelManager.FilePathResxChanged += filresx =>
            {
                Dispatcher.Invoke(UpdateChartVisibility);
            };
        }
        public PageResultats ParentPage { get; set; }

        // Référence vers la page mère si nécessaire


        // Méthode pour générer et afficher la CrossTable
        public Task GenerateAndDisplayCrossTable(string resuxFilePath)
        {
            if (string.IsNullOrEmpty(resuxFilePath))
            {
                MessageBox.Show("Le chemin d'accès est vide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return Task.CompletedTask;
            }

            try
            {
                DataTable table = GetTableCrossTable_InfluenceSimple(resuxFilePath); // Appel via la page mère
                ResultsCrossTable.AutoGenerateColumns = true;
                ResultsCrossTable.AutoGeneratingColumn += ResultsCrossTable_AutoGeneratingColumn;
                ResultsCrossTable.ItemsSource = table.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur est survenue : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return Task.CompletedTask;
        }

        private void BtnGenererGSheet_Click(object sender, RoutedEventArgs e)
        {
            // Appel méthode export GSheet de la page mère ou interne
        }
        private void UpdateChartVisibility()
        {
            bool hasData = ModelManager.FilePathResx != null && ModelManager.FilePathResx.Any();

            NoDataImage.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
            ResultsCrossTable.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;

        }

        #region Exportation des résultats EXCEL

        #region Tables generations

        public enum CrossTableVariant
        {
            InfluenceByContact, // Influence des contacts
            ContribByContact, // Contribution des contacts
            ContribByTolDetailed, // Contribution des tolérances detaillée
            ContribByTolCondensed // Contribution des tolérances condensée
        }

        // Méthode générique factorisée : construit une cross-table selon une "projection".
        // - resuxFilePath : chemin des résultats (comme avant)
        // - valueProjection : pour une entrée (dynamic) retourne une séquence (ColumnSuffix, valeur)
        //   Exemple de suffixeColonne : "" (aucun suffixe), " - tol1", " - tol2", etc.
        // - baseColumnName : nom de la colonne contact (par défaut "Contact")
        // - missingValuePlaceholder : texte utilisé quand pas de valeur
        public DataTable GetTableCrossTable(string resuxFilePath, Func<dynamic, object> valueProjection, string baseColumnName = "Contact", string missingValuePlaceholder = "-")
        {
            // Étape 1 : Extraction des exigences
            var reqHeaders = _resuxSerializer.ExtractReqHeaders(resuxFilePath);

            // Étape 2 : Charger les résultats
            var allResults = reqHeaders
                .Select(r => (r.IdReq, Result: _resuxSerializer.LoadInfluencedWCFromFile(r.IdReq, resuxFilePath)))
                .ToList();

            // Étape 3 : Construire la DataTable
            DataTable table = new();
            table.Columns.Add(baseColumnName, typeof(string));

            // Colonnes P1 et P2 (texte)
            table.Columns.Add("P1", typeof(string));
            table.Columns.Add("P2", typeof(string));

            // Colonnes exigences (numériques)
            foreach (var req in reqHeaders)
                table.Columns.Add(req.Name, typeof(double));

            // Étape 4 : déterminer tous les contacts uniques par IdData
            var allDataEntries = allResults
                .SelectMany(r => r.Result.Data)
                .GroupBy(d => d.IdData) // clé unique : IdData
                .Select(g => g.First())
                .OrderBy(d => d.IdData)
                .ToList();

            // Étape 5 : remplir les lignes
            foreach (var entry in allDataEntries)
            {
                var row = table.NewRow();

                // Affichage : si NameData vide, afficher un texte avec ID
                row[baseColumnName] = string.IsNullOrWhiteSpace(entry.NameData)
                    ? $"NO name ID={entry.IdData}"
                    : entry.NameData;

                // Ajouter les valeurs pour chaque exigence
                foreach (var req in reqHeaders)
                {
                    var entryForReq = allResults
                        .First(r => r.IdReq == req.IdReq)
                        .Result.Data
                        .FirstOrDefault(d => d.IdData == entry.IdData);

                    if (entryForReq != null)
                    {
                        try
                        {
                            var value = valueProjection(entryForReq);

                            if (value == null)
                                row[req.Name] = DBNull.Value;
                            else
                                row[req.Name] = Convert.ToDouble(value); // garder le type double
                        }
                        catch
                        {
                            row[req.Name] = DBNull.Value;
                        }
                    }
                    else
                    {
                        row[req.Name] = DBNull.Value;
                    }
                }

                // Colonnes PartOri et PartExtre (texte)
                row["P1"] = entry.NameExtre ?? missingValuePlaceholder;
                row["P2"] = entry.NameOri ?? missingValuePlaceholder;

                table.Rows.Add(row);
            }

            return table;
        }


        public DataTable GetTableCrossTable_InfluenceSimple(string resuxFilePath)
        {
            return GetTableCrossTable(
                resuxFilePath,
                // projection : retourne directement la valeur InfluencWC
                entry => entry.InfluencWC
            );
        }


        // Variante : contributions par tolérance quand l'entrée expose ContribByTolerance (double[])
        public DataTable GetTableCrossTable_ContribByContact(string resuxFilePath)
        {
            return GetTableCrossTable(
                resuxFilePath,
                entry =>
                {
                    if (entry == null)
                        return "-";

                    try
                    {
                        // Somme des contributions absolues pour ce contact
                        double total = 0;
                        if (!double.IsNaN(entry.ContribWCOri))
                            total += Math.Abs(entry.ContribWCOri);
                        if (!double.IsNaN(entry.ContribWCInt))
                            total += Math.Abs(entry.ContribWCInt);
                        if (!double.IsNaN(entry.ContribWCExtr))
                            total += Math.Abs(entry.ContribWCExtr);

                        return total;
                    }
                    catch
                    {
                        return "-";
                    }
                },
                baseColumnName: "Contact",
                missingValuePlaceholder: "-"
            );
        }


        /// <summary>
        /// Génère un tableau croisé des contributions entre tolérances et exigences.
        /// - En mode condensé : les tolérances sont regroupées par nom.
        /// - En mode détaillé : chaque tolérance est distinguée par son contexte (DataName + Type).
        /// </summary>
        /// <param name="resuxFilePath">Chemin du fichier .resux à lire.</param>
        /// <param name="detailedView">Si vrai, ajoute le contexte (NameData + Type).</param>
        /// <returns>DataTable prête pour affichage.</returns>
        public DataTable GetTableTolVsReq(string resuxFilePath, bool detailedView = false)
        {
            // --- Étape 1 : Extraction des exigences ---
            var reqHeaders = _resuxSerializer.ExtractReqHeaders(resuxFilePath);

            var allResults = reqHeaders
                .Select(r => (r.IdReq, Result: _resuxSerializer.LoadInfluencedWCFromFile(r.IdReq, resuxFilePath)))
                .ToList();

            // --- Étape 2 : Collecte de toutes les tolérances et leur contexte ---
            var allTolContexts = allResults
                .SelectMany(t => t.Result.Data.SelectMany(entry => new[]
                {
                    (TolInfo: entry.TolExtrInfo, Type: "P1", entry.NameData, entry.ContribWCExtr , IdReq: t.IdReq),
                    (TolInfo: entry.TolIntInfo, Type: "Int", entry.NameData, entry.ContribWCInt, IdReq: t.IdReq),
                    (TolInfo: entry.TolOriInfo , Type: "P2", entry.NameData, entry.ContribWCOri, IdReq: t.IdReq)
                }))
                .Where(x => x.TolInfo != null && !string.IsNullOrWhiteSpace(x.TolInfo.Name))
                .ToList();

            // --- Étape 3 : Détermination des clés d’identification des tolérances ---
            var allTolKeys = allTolContexts
                .Select(x =>
                    detailedView
                        ? (Key: $"{x.TolInfo.Name.Trim()} | {x.NameData} - {x.Type}",
                           Name: x.TolInfo.Name.Trim(),
                           Context: $"{x.NameData} - {x.Type}")
                        : (Key: x.TolInfo.Name.Trim(),
                           Name: x.TolInfo.Name.Trim(),
                           Context: string.Empty))
                .Distinct()
                .OrderBy(k => k.Key)
                .ToList();

            // --- Étape 4 : Construction de la DataTable ---
            DataTable table = new();
            table.Columns.Add("Nom Tolérance", typeof(string));
            if (detailedView)
                table.Columns.Add("Contexte", typeof(string));

            foreach (var req in reqHeaders)
                table.Columns.Add(req.Name, typeof(string));

            // --- Étape 5 : Remplissage des lignes ---
            foreach (var tolKey in allTolKeys)
            {
                var row = table.NewRow();
                row["Nom Tolérance"] = tolKey.Name;
                if (detailedView)
                    row["Contexte"] = tolKey.Context;

                foreach (var req in reqHeaders)
                {
                    var reqId = req.IdReq;

                    // Sélection des entrées correspondant à la tolérance courante et à l’exigence
                    var matches = allTolContexts.Where(x =>
                        x.IdReq == reqId &&
                        (
                            (!detailedView && x.TolInfo.Name.Trim() == tolKey.Name)
                            || (detailedView && $"{x.TolInfo.Name.Trim()} | {x.NameData} - {x.Type}" == tolKey.Key)
                        )
                    ).ToList();

                    if (matches.Count > 0)
                    {
                        // Somme des contributions pour cette tolérance et exigence
                        double total = matches.Sum(m => m.Item4);
                        row[req.Name] = total.ToString("0.000", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        row[req.Name] = "-";
                    }
                }

                table.Rows.Add(row);
            }
            return table;
        }

        #endregion

        // Mise à jour de votre handler de bouton : exemple d'appel
        private void ButtonGenererCrossTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataTable table1 = GetTableCrossTable_InfluenceSimple(ModelManager.FilePathResx);
                DataTable table2 = GetTableTolVsReq(ModelManager.FilePathResx, true);
                DataTable table3 = GetTableTolVsReq(ModelManager.FilePathResx, false);
                DataTable table4 = GetTableCrossTable_ContribByContact(ModelManager.FilePathResx);

                // Vérification qu’au moins une table contient des données
                bool hasData =
                    (table1 != null && table1.Rows.Count > 0) ||
                    (table2 != null && table2.Rows.Count > 0) ||
                    (table3 != null && table3.Rows.Count > 0);

                if (!hasData)
                {
                    MessageBox.Show("Aucune donnée à exporter.", "Export", MessageBoxButton.OK);
                    return;
                }

                // Chemin de sauvegarde 
                string directoryPath = GetPathWithFolderBrowserDialog();

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                string fileName = $"TableCroisee_{timestamp}.xlsx";

                if (directoryPath != null && fileName != null)
                {
                    ExportCrossTableToExcel(table1, directoryPath, fileName, "InflByContact");
                    ExportCrossTableToExcel(table2, directoryPath, fileName, "TolCond");
                    ExportCrossTableToExcel(table3, directoryPath, fileName, "TolDetail");
                    ExportCrossTableToExcel(table4, directoryPath, fileName, "ContribByContact");
                }


                MessageBox.Show($"Le rapport a été créé avec succès :\n{directoryPath}", "Export terminé", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur est survenue : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Fenetre pour choisir le chemin pour l'export de l'excel (inchangé)
        private string GetPathWithFolderBrowserDialog()
        {
            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderDialog.Description = "Sélectionnez un dossier pour sauvegarder le classeur Excel.";
                folderDialog.SelectedPath = ModelManager.AppDataPath; // Dossier par défaut

                var result = folderDialog.ShowDialog();

                return result == System.Windows.Forms.DialogResult.OK
                    ? folderDialog.SelectedPath
                    : null;
            }
        }

        // Chargement du template (inchangé)
        private XLWorkbook LoadTemplateExcelCdC()
        {
            string PathTemplate = "TOLTECH_APPLICATION.Asset.Template_CdC_Excel_Toltech.xlsx";
            var assembly = Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(PathTemplate);
            if (stream == null)
                throw new FileNotFoundException($"Ressource introuvable : {PathTemplate}");

            return new XLWorkbook(stream); // Do not dispose the stream here
        }

        // Export Excel modifié : écrit les nombres en tant que double quand possible
        public string ExportCrossTableToExcel(DataTable table, string directoryPath, string fileName = null, string sheetName = null)
        {
            if (table == null || table.Rows.Count == 0)
                throw new ArgumentException("La table est vide ou nulle.");

            // Valeur par défaut pour fileName si non fournie
            if (string.IsNullOrEmpty(fileName))
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                fileName = $"TableCroisee_{timestamp}.xlsx";
            }

            // Nom par défaut de la feuille (au cas où)
            sheetName ??= $"Table_{DateTime.Now:HHmmss}";

            // Nom de fichier final (unique par session)
            var fullPath = Path.Combine(directoryPath, fileName);

            // Si le fichier existe déjà, on le réutilise
            XLWorkbook workbook;
            if (File.Exists(fullPath))
            {
                workbook = new XLWorkbook(fullPath);
            }
            else
            {
                workbook = LoadTemplateExcelCdC() ?? new XLWorkbook();
            }

            IXLWorksheet worksheet;
            // Si une feuille du même nom existe déjà, on réutilise sinon on la crée
            var existingSheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));

            if (existingSheet != null)
            {
                worksheet = existingSheet;
            }
            else
            {
                worksheet = workbook.Worksheets.Add(sheetName);
            }

            int offset = 8; // Ligne 9 => index 8 car ClosedXML commence à 1 pour Cell

            // --- Écriture des en-têtes ---
            for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
            {
                worksheet.Cell(offset + 1, colIndex + 1).Value = table.Columns[colIndex].ColumnName;
                worksheet.Cell(offset + 1, colIndex + 1).Style.Font.Bold = true;
            }

            // --- Écriture des données ---
            for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
                {
                    var raw = row[colIndex]?.ToString();
                    if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                    {
                        worksheet.Cell(offset + 2 + rowIndex, colIndex + 1).Value = parsed;
                        worksheet.Cell(offset + 2 + rowIndex, colIndex + 1).Style.NumberFormat.Format = "#,##0.0000";
                    }
                    else
                    {
                        worksheet.Cell(offset + 2 + rowIndex, colIndex + 1).Value = raw ?? string.Empty;
                    }
                }
            }

            int firstDataRow = offset + 2;
            int lastDataRow = offset + 1 + table.Rows.Count;

            // --- Ajouter la ligne de somme pour les colonnes numériques ---
            int sumRowIndex = lastDataRow + 1;
            worksheet.Cell(sumRowIndex, 1).Value = "TOTAL"; // label
            worksheet.Cell(sumRowIndex, 1).Style.Font.Bold = true;

            for (int colIndex = 1; colIndex < table.Columns.Count; colIndex++) // à partir de la 2e colonne
            {
                if (table.Columns[colIndex].DataType == typeof(double) || table.Columns[colIndex].DataType == typeof(float) || table.Columns[colIndex].DataType == typeof(decimal))
                {
                    string colLetter = worksheet.Column(colIndex + 1).ColumnLetter();
                    worksheet.Cell(sumRowIndex, colIndex + 1).FormulaA1 = $"SUM({colLetter}{firstDataRow}:{colLetter}{lastDataRow})";
                    worksheet.Cell(sumRowIndex, colIndex + 1).Style.NumberFormat.Format = "#,##0.0000";
                    worksheet.Cell(sumRowIndex, colIndex + 1).Style.Font.Bold = true;
                }
            }

            // --- Définir la plage complète de la table Excel ---
            var tableRange = worksheet.Range(
                offset + 1, 1, // première cellule de la table (A9)
                offset + 1 + table.Rows.Count, table.Columns.Count // dernière cellule
            );

            // --- Créer la table Excel ---
            var excelTable = tableRange.CreateTable();
            excelTable.Name = $"CrossTable_{sheetName.Replace(" ", "_")}_{DateTime.Now:HHmmss}";
            excelTable.ShowAutoFilter = true;
            excelTable.Theme = XLTableTheme.TableStyleMedium2; // tu peux changer le style


            // --- Ajustement automatique des colonnes ---
            worksheet.Columns().AdjustToContents();

            // --- Sauvegarde finale ---
            workbook.SaveAs(fullPath);

            return fullPath;
        }


        #endregion

        #region Helper

        // Helper pour libellés lisibles dans la ComboBox
        private string GetCrossTableVariantDisplayName(CrossTableVariant v)
        {
            return v switch
            {
                CrossTableVariant.InfluenceByContact => "Influence par contact",
                CrossTableVariant.ContribByContact => "Contribution par contact (somme)",
                CrossTableVariant.ContribByTolDetailed => "Tolérances — Vue détaillée",
                CrossTableVariant.ContribByTolCondensed => "Tolérances — Vue condensée",
                _ => v.ToString()
            };
        }

        // Générateur centralisé pour chaque variante
        private DataTable GenerateTableForVariant(CrossTableVariant variant, string resuxFilePath)
        {
            return variant switch
            {
                CrossTableVariant.InfluenceByContact => GetTableCrossTable_InfluenceSimple(resuxFilePath),
                CrossTableVariant.ContribByContact => GetTableCrossTable_ContribByContact(resuxFilePath),
                CrossTableVariant.ContribByTolDetailed => GetTableTolVsReq(resuxFilePath, detailedView: true),
                CrossTableVariant.ContribByTolCondensed => GetTableTolVsReq(resuxFilePath, detailedView: false),
                _ => GetTableCrossTable_InfluenceSimple(resuxFilePath)
            };
        }

        private async void CbCrossTableVariant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbCrossTableVariant.SelectedValue is CrossTableVariant selectedVariant)
            {
                if (string.IsNullOrEmpty(ModelManager.FilePathResx))
                {
                    ResultsCrossTable.ItemsSource = null;
                    return;
                }

                try
                {
                    // Générer la table correspondant à la variante sélectionnée
                    DataTable table = await Task.Run(() => GenerateTableForVariant(selectedVariant, ModelManager.FilePathResx));

                    // Lier la table au DataGrid
                    ResultsCrossTable.AutoGenerateColumns = true;
                    ResultsCrossTable.AutoGeneratingColumn += ResultsCrossTable_AutoGeneratingColumn;
                    ResultsCrossTable.ItemsSource = table.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la génération de la CrossTable : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ResultsCrossTable_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Vérifie le type de la colonne
            if (e.PropertyType == typeof(double) || e.PropertyType == typeof(float) || e.PropertyType == typeof(decimal))
            {
                if (e.Column is DataGridTextColumn col && col.Binding is Binding binding)
                {
                    // Format numérique uniforme (exemple : 4 décimales)
                    binding.StringFormat = "F3";

                    // Réaffectation du binding modifié
                    col.Binding = binding;
                }
            }

            // Centrer le contenu pour homogénéiser le rendu
            if (e.Column is DataGridTextColumn textCol)
            {
                textCol.ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
            {
                new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
            }
                };
            }
        }


        #endregion




    }
}
