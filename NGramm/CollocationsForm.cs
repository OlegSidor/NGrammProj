using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;
using NGramm;

namespace NGramm
{

    public partial class CollocationsForm : Form
    {
        private TextBox inputNgramTextBox;
        private NumericUpDown nPrimeSelector;
        private ComboBox modeSelector;
        private CheckBox includeSpacesCheckBox;
        private Button searchButton;
        private Button saveButton;
        private ListView resultsListView;
        private readonly NgrammProcessor processor;
        private int fMin;
        private readonly bool useSpaces;
        private Label nPrimeLabel;
        private Label inputLabel;

        private readonly int maxNValue;
        public CollocationsForm(NgrammProcessor processor, int fMin, bool useSpaces, int maxNValue)
        {
            this.processor = processor;
            this.fMin = fMin;
            this.useSpaces = useSpaces;
            this.maxNValue = maxNValue;
            InitializeComponents();

            nPrimeSelector.Maximum = maxNValue;
            nPrimeSelector.Value = Math.Min(maxNValue, nPrimeSelector.Maximum);

        }

        private void ModeSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            var modesWithNPrime = new HashSet<int> { 0, 3, 6 };
            var modesWithoutInput = new HashSet<int> { };

            bool showNPrime = modesWithNPrime.Contains(modeSelector.SelectedIndex);
            bool showInput = !modesWithoutInput.Contains(modeSelector.SelectedIndex);
            nPrimeSelector.Visible = showNPrime;
            nPrimeLabel.Visible = showNPrime;
            inputNgramTextBox.Visible = showInput;
            inputLabel.Visible = showInput;
        }

        private void InitializeComponents()
        {
            this.Text = "Пошук колокацій";
            this.Size = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            inputLabel = new Label { Text = "Введіть n-граму:", Location = new Point(20, 20), AutoSize = true };
            inputNgramTextBox = new TextBox { Location = new Point(150, 16), Width = 200 };

            nPrimeLabel = new Label { Text = "n':", Location = new Point(370, 20), AutoSize = true };
            nPrimeSelector = new NumericUpDown { Location = new Point(410, 16), Width = 60, Minimum = 1, Maximum = 20, Value = 3 };

            Label modeLabel = new Label { Text = "Режим:", Location = new Point(20, 60), AutoSize = true };
            modeSelector = new ComboBox { Location = new Point(150, 56), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList };
            modeSelector.Items.AddRange(new string[]
            {
                "Буквена n-грама в n'-грамах",
                "Буквена n-грама в словах",
                "Буквена n-грама в реченнях",
                "Символьна n-грама в n'-грамах",
                "Символьна n-грама в словах",
                "Символьна n-грама в реченнях",
                "Словесна n-грама в словесних n'-грамах",
                "Словесна n-грама в реченнях"
            });
            modeSelector.SelectedIndex = 0;
            modeSelector.SelectedIndexChanged += ModeSelector_SelectedIndexChanged;


            searchButton = new Button { Text = "Пошук", Location = new Point(600, 16), Width = 150 };
            searchButton.Click += SearchButton_Click;

            saveButton = new Button { Text = "Зберегти результати", Location = new Point(600, 56), Width = 150 };
            saveButton.Click += SaveButton_Click;

            resultsListView = new ListView
            {
                Location = new Point(20, 140),
                Size = new Size(740, 400),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            resultsListView.Columns.Add("#", 50);
            resultsListView.Columns.Add("Кількість", 500);
            resultsListView.Columns.Add("Частота", 150);

            this.Controls.Add(inputLabel);
            this.Controls.Add(inputNgramTextBox);
            this.Controls.Add(nPrimeLabel);
            this.Controls.Add(nPrimeSelector);
            this.Controls.Add(modeLabel);
            this.Controls.Add(modeSelector);
            this.Controls.Add(includeSpacesCheckBox);
            this.Controls.Add(searchButton);
            this.Controls.Add(saveButton);
            this.Controls.Add(resultsListView);

            resultsListView.ColumnClick += ResultsListView_ColumnClick;
            resultsListView.MouseClick += ResultsListView_MouseClick;
        }

        private string NormalizeSpaces(string text)
        {
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {

            string ngram = useSpaces
                ? inputNgramTextBox.Text.Replace('_', ' ')
                : inputNgramTextBox.Text.Trim().Replace('_', ' ');
            if (string.IsNullOrWhiteSpace(ngram))
            {
                MessageBox.Show("Поле n-грами не може бути порожнім.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            resultsListView.Items.Clear();

            int nPrime = (int)nPrimeSelector.Value;
            int mode = modeSelector.SelectedIndex;
            NgrammProcessor.process_spaces = useSpaces;

            if (!useSpaces && (mode >= 0 && mode <= 5) && ngram.Contains(" "))
            {
                MessageBox.Show("Уведена n-грама містить пробіли, але опція 'враховувати пробіли' неактивна.\n" +
                                "Увімкніть опцію або приберіть пробіли з n-грами.",
                                "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int textLength = processor.endsignedTextorg.Length;
            if (fMin > 1)
            {
                if (textLength < 1000)
                    fMin = 1;
                else if (textLength < 5000)
                    fMin = Math.Min(fMin, 2);
                else if (textLength < 20000)
                    fMin = Math.Min(fMin, 3);
                else if (textLength < 50000)
                    fMin = Math.Min(fMin, 5);
            }

            switch (mode)
            {
                case 0:
                case 1:
                case 2:
                    if (processor.GetLiteralNgrams().Count == 0)
                    {
                        MessageBox.Show("Буквені n-грами ще не пораховані.");
                        return;
                    }
                    break;

                case 3:
                case 4:
                case 5:
                    if (processor.GetSymbolNgrams().Count == 0)
                    {
                        MessageBox.Show("Символьні n-грами ще не пораховані.");
                        return;
                    }
                    break;

                case 6:
                case 7:
                    if (processor.GetWordsNgrams().Count == 0)
                    {
                        MessageBox.Show("Словесні n-грами ще не пораховані.");
                        return;
                    }
                    break;
            }

            if (mode == 1 || mode == 2 || mode == 4 || mode == 5)
            {

                if (ngram.Length > maxNValue)
                {
                    MessageBox.Show($"Довжина n-грами ({ngram.Length}) перевищує n ({maxNValue}), яке було встановлено при обчисленні.",
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            if (mode == 0 || mode == 3)
            {
                if (ngram.Length > nPrime)
                {
                    MessageBox.Show($"Довжина n-грами ({ngram.Length}) перевищує n' ({nPrime}).", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (mode == 7 || mode == 6)
            {

                int wordCount = ngram.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (mode == 6 && wordCount > nPrime)
                {
                    MessageBox.Show($"Кількість слів ({wordCount}) перевищує n` ({nPrime}), яке було встановлено при обчисленні.",
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (mode == 7 && wordCount > maxNValue)
                {
                    MessageBox.Show($"Кількість слів ({wordCount}) перевищує n ({maxNValue}), яке було встановлено при обчисленні.",
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (modeSelector.SelectedIndex == 6 || modeSelector.SelectedIndex == 7)
            {
                string rawInput = inputNgramTextBox.Text.Replace('_', ' ');


                if (string.IsNullOrWhiteSpace(rawInput))
                {
                    MessageBox.Show("Поле n-грами не може бути порожнім або містити лише пробіли.",
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }


                if (rawInput.StartsWith(" ") || rawInput.EndsWith(" "))
                {
                    MessageBox.Show("Введена n-грама має пробіли на початку або в кінці.",
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }


                if (rawInput.Contains("  "))
                {
                    MessageBox.Show("Введена n-грама містить подвійні пробіли всередині.",
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string[] words = rawInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Regex validWord = new Regex(@"^[a-zA-Zа-яА-Я0-9]+$");

                foreach (string word in words)
                {
                    if (!validWord.IsMatch(word))
                    {
                        MessageBox.Show($"Некоректна n-грама: слово \"{word}\" містить недопустимі символи.\n" +
                            "Слова можуть містити лише літери.",
                            "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }


            resultsListView.Items.Clear();
            Dictionary<string, int> result = new Dictionary<string, int>();

            switch (mode)
            {
                case 0:
                    foreach (var cont in processor.GetLiteralNgrams().Where(c => c.n == nPrime))
                    {
                        foreach (var kv in cont.GetNgrams(fMin))
                        {
                            string key = NgrammProcessor.ignore_case ? kv.Key.ToLower() : kv.Key;
                            string cmp = NgrammProcessor.ignore_case ? ngram.ToLower() : ngram;
                            if (key.Contains(cmp))
                                result[kv.Key] = kv.Value;
                        }
                    }
                    break;

                case 1:
                    foreach (var word in processor.Words(processor.unsignedTextorg))
                    {
                        string key = NgrammProcessor.ignore_case ? word.Value.ToLower() : word.Value;
                        string cmp = NgrammProcessor.ignore_case ? ngram.ToLower() : ngram;
                        if (key.Contains(cmp))
                        {
                            if (!result.ContainsKey(word.Value))
                                result[word.Value] = 0;
                            result[word.Value]++;
                        }
                    }
                    break;

                case 2:
                    foreach (var sentence in processor.endsignedTextorg.Split(new[] { '.', '?', '!', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string key = NgrammProcessor.ignore_case ? sentence.ToLower() : sentence;
                        string cmp = NgrammProcessor.ignore_case ? ngram.ToLower() : ngram;
                        if (key.Contains(cmp))
                        {
                            if (!result.ContainsKey(sentence))
                                result[sentence] = 0;
                            result[sentence]++;
                        }
                    }
                    break;

                case 3:
                    foreach (var cont in processor.GetSymbolNgrams().Where(c => c.n == nPrime))
                    {
                        foreach (var kv in cont.GetNgrams(fMin))
                        {
                            string key = NgrammProcessor.ignore_case ? kv.Key.ToLower() : kv.Key;
                            string cmp = NgrammProcessor.ignore_case ? ngram.ToLower() : ngram;
                            if (key.Contains(cmp))
                                result[kv.Key] = kv.Value;
                        }
                    }
                    break;

                case 4:
                    foreach (var word in processor.Words(processor.rawTextorg))
                    {
                        string key = NgrammProcessor.ignore_case ? word.Value.ToLower() : word.Value;
                        string cmp = NgrammProcessor.ignore_case ? ngram.ToLower() : ngram;
                        if (key.Contains(cmp))
                        {
                            if (!result.ContainsKey(word.Value))
                                result[word.Value] = 0;
                            result[word.Value]++;
                        }
                    }
                    break;

                case 5:
                    foreach (var sentence in processor.rawTextorg.Split(new[] { '.', '?', '!', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string key = NgrammProcessor.ignore_case ? sentence.ToLower() : sentence;
                        string cmp = NgrammProcessor.ignore_case ? ngram.ToLower() : ngram;
                        if (key.Contains(cmp))
                        {
                            if (!result.ContainsKey(sentence))
                                result[sentence] = 0;
                            result[sentence]++;
                        }
                    }
                    break;


                case 6:
                    {
                        bool ignorePunctuation = NgrammProcessor.ignore_punctuation;
                        var targetWords = processor.Words(ngram, true)
                           .Select(x => NgrammProcessor.ignore_case ? x.Value.ToLower() : x.Value)
                           .ToArray();


                        if (targetWords.Length > nPrime)
                        {
                            MessageBox.Show($"[ПОМИЛКА] n′ ({nPrime}) не може бути менше кількості слів у введеній n-грами ({targetWords.Length}).");
                            return;
                        }


                        var containers = processor.GetWordsNgrams();

                        int candidates = 0;
                        int checkedCandidates = 0;

                        foreach (var cont in containers)
                        {
                            if (cont.n != nPrime)
                                continue;

                            var ngrams = cont.GetNgrams(fMin).ToList();

                            if (!ngrams.Any())
                                MessageBox.Show($"[ПОПЕРЕДЖЕННЯ] У контейнері n={nPrime} GetNgrams(fMin={fMin}) повернув 0 n-грам. Можливо, всі частоти = 1?");

                            foreach (var kv in ngrams)
                            {
                                string candidateNormalized = NormalizeSpaces(NgrammProcessor.ignore_case ? kv.Key.ToLower() : kv.Key);
                                string[] candidateWords = candidateNormalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                checkedCandidates++;

                                bool allWordsFound = true;
                                foreach (string targetWord in targetWords)
                                {
                                    if (!candidateWords.Contains(targetWord))
                                    {
                                        allWordsFound = false;
                                        break;
                                    }
                                }

                                if (allWordsFound)
                                {
                                    result[kv.Key] = kv.Value;
                                    candidates++;
                                }
                            }
                        }
                        break;
                    }
                case 7:
                    {
                        fMin = 1;
                        if (string.IsNullOrWhiteSpace(processor.rawTextorg))
                        {
                            MessageBox.Show("Текст не завантажений або пустий.");
                            return;
                        }

                        var inputWords = processor.Words(ngram, ignoreCode: true)
                                                  .Select(x => NgrammProcessor.ignore_case ? x.Value.ToLower() : x.Value)
                                                  .ToArray();

                        if (inputWords.Length > maxNValue)
                        {
                            MessageBox.Show($"Кількість слів у n-грамі ({inputWords.Length}) перевищує n ({maxNValue}).");
                            return;
                        }


                        char[] delimiters = { '.', '?', '!', '\n' };
                        string[] sentences = processor.rawTextorg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var sentence in sentences)
                        {
                            string cleanedSentence = sentence.Trim();
                            if (string.IsNullOrWhiteSpace(cleanedSentence))
                                continue;

                            var sentenceWords = processor.Words(cleanedSentence, ignoreCode: true)
                                                         .Select(x => NgrammProcessor.ignore_case ? x.Value.ToLower() : x.Value)
                                                         .ToHashSet();

                            if (inputWords.All(w => sentenceWords.Contains(w)))
                            {
                                if (!result.ContainsKey(cleanedSentence))
                                    result[cleanedSentence] = 0;
                                result[cleanedSentence]++;
                            }
                        }

                        break;
                    }




            }

            var sorted = result.Where(r => r.Value >= fMin).OrderByDescending(r => r.Value).ToList();
            int rank = 1;
            foreach (var kv in sorted)
            {
                var item = new ListViewItem(rank.ToString());
                item.SubItems.Add(kv.Key);
                item.SubItems.Add(kv.Value.ToString());
                resultsListView.Items.Add(item);
                rank++;
            }

            MessageBox.Show("Пошук завершено", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Text files (*.txt)|*.txt";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (var writer = new StreamWriter(sfd.FileName, false, Encoding.UTF8))
                    {
                        writer.WriteLine("#\tItem\tFrequency");
                        foreach (ListViewItem item in resultsListView.Items)
                        {
                            writer.WriteLine($"{item.SubItems[0].Text}\t{item.SubItems[1].Text}\t{item.SubItems[2].Text}");
                        }
                    }
                }
            }
        }

        private void ResultsListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            resultsListView.ListViewItemSorter = new CollocationListViewSorter(e.Column);
        }
        private void ResultsListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var info = resultsListView.HitTest(e.Location);

                if (info.Item != null)
                {
                    string toCopy = info.Item.SubItems.Count > 1 ? info.Item.SubItems[1].Text : info.Item.Text;
                    Clipboard.SetText(toCopy);

                    MessageBox.Show("Скопійовано: " + toCopy, "Буфер обміну", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }


    }
}
