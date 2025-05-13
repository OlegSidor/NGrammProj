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
        private readonly int fMin;
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
        }

        private void ModeSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            var modesWithNPrime = new HashSet<int> { 0, 3, 6 };
            var modesWithoutInput = new HashSet<int> { }; // додай індекси, де не треба введення вручну

            bool showNPrime = modesWithNPrime.Contains(modeSelector.SelectedIndex);
            bool showInput = !modesWithoutInput.Contains(modeSelector.SelectedIndex); // якщо треба приховувати в деяких режимах

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
                "Словесна н-грама в словесних n'-грамах",
                "Словесна п-грама в реченнях"
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
        }

        private string NormalizeSpaces(string text)
        {
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            string ngram = inputNgramTextBox.Text.Trim().Replace('_', ' ');
            int nPrime = (int)nPrimeSelector.Value;
            int mode = modeSelector.SelectedIndex;
            NgrammProcessor.process_spaces = useSpaces;

            if (!CheckNgramsExist(mode)) return;
            if (!CheckCharacterNgramLength(ngram, nPrime, maxNValue, mode)) return;
            if ((mode == 6 || mode == 7) && !CheckWordNgram(ngram, nPrime, maxNValue, mode)) return;
            if ((mode == 6 || mode == 7) && !ValidateWordNgramInput(inputNgramTextBox.Text.Replace('_', ' '))) return;

            resultsListView.Items.Clear();
            Dictionary<string, int> result = new Dictionary<string, int>();

            switch (mode)
            {
                case 0:
                    foreach (var cont in processor.GetLiteralNgrams().Where(c => c.n == nPrime))
                        foreach (var kv in cont.GetNgrams(fMin))
                            if ((NgrammProcessor.ignore_case ? kv.Key.ToLower() : kv.Key)
                                .Contains(NgrammProcessor.ignore_case ? ngram.ToLower() : ngram))
                                result[kv.Key] = kv.Value;
                    break;
                case 1:
                    foreach (var word in processor.Words(processor.unsignedTextorg))
                        if ((NgrammProcessor.ignore_case ? word.Value.ToLower() : word.Value)
                            .Contains(NgrammProcessor.ignore_case ? ngram.ToLower() : ngram))
                            result[word.Value] = result.ContainsKey(word.Value) ? result[word.Value] + 1 : 1;
                    break;
                case 2:
                    var text2 = NgrammProcessor.ignore_punctuation ? processor.unsignedTextorg : processor.endsignedTextorg;
                    foreach (var sentence in Regex.Split(text2, @"(\r?\n){2,}")
                             .Where(s => !string.IsNullOrWhiteSpace(s)))
                        if ((NgrammProcessor.ignore_case ? sentence.ToLower() : sentence)
                            .Contains(NgrammProcessor.ignore_case ? ngram.ToLower() : ngram))
                            result[sentence] = result.ContainsKey(sentence) ? result[sentence] + 1 : 1;
                    break;
                case 3:
                    foreach (var cont in processor.GetSymbolNgrams().Where(c => c.n == nPrime))
                        foreach (var kv in cont.GetNgrams(fMin))
                            if ((NgrammProcessor.ignore_case ? kv.Key.ToLower() : kv.Key)
                                .Contains(NgrammProcessor.ignore_case ? ngram.ToLower() : ngram))
                                result[kv.Key] = kv.Value;
                    break;
                case 4:
                    foreach (var word in processor.Words(processor.rawTextorg))
                        if ((NgrammProcessor.ignore_case ? word.Value.ToLower() : word.Value)
                            .Contains(NgrammProcessor.ignore_case ? ngram.ToLower() : ngram))
                            result[word.Value] = result.ContainsKey(word.Value) ? result[word.Value] + 1 : 1;
                    break;
                case 5:
                    foreach (var sentence in Regex.Split(processor.rawTextorg, @"(\r?\n){2,}")
                             .Where(s => !string.IsNullOrWhiteSpace(s)))

                        if ((NgrammProcessor.ignore_case ? sentence.ToLower() : sentence)
                            .Contains(NgrammProcessor.ignore_case ? ngram.ToLower() : ngram))
                            result[sentence] = result.ContainsKey(sentence) ? result[sentence] + 1 : 1;
                    break;
                case 6:
                    string normalizedInput = NormalizeSpaces(NgrammProcessor.ignore_case ? ngram.ToLower() : ngram);
                    string[] targetWords = normalizedInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var cont in processor.GetWordsNgrams().Where(c => c.n == nPrime))
                        foreach (var kv in cont.GetNgrams(fMin))
                        {
                            string candidateNormalized = NormalizeSpaces(NgrammProcessor.ignore_case ? kv.Key.ToLower() : kv.Key);
                            string[] candidateWords = candidateNormalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (ContainsSubsequence(candidateWords, targetWords))
                                result[kv.Key] = kv.Value;
                        }
                    break;
                case 7: // Лексична n-грама в реченнях
                    {
                        normalizedInput = NormalizeSpaces(NgrammProcessor.ignore_case ? ngram.ToLower() : ngram);
                        int wordCount = normalizedInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

                        if (wordCount > maxNValue)
                        {
                            MessageBox.Show($"Кількість слів у n-грамі ({wordCount}) перевищує n ({maxNValue}), яке було встановлено при обчисленні.",
                                "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        foreach (var sentence in processor.endsignedTextorg
                                 .Split(new[] { '.', '?', '!', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string keySentence = NormalizeSpaces(NgrammProcessor.ignore_case ? sentence.ToLower() : sentence);
                            string[] sentenceWords = keySentence.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string[] inputWords = normalizedInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            // ✅ Ось основна перевірка як в case 6
                            if (ContainsSubsequence(sentenceWords, inputWords))
                            {
                                if (!result.ContainsKey(sentence))
                                    result[sentence] = 0;
                                result[sentence]++;
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

        //fdgfdgdfg
        private bool ContainsSubsequence(string[] source, string[] pattern)
        {
            for (int i = 0; i <= source.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    string a = NgrammProcessor.ignore_case ? source[i + j].ToLower() : source[i + j];
                    string b = NgrammProcessor.ignore_case ? pattern[j].ToLower() : pattern[j];
                    if (a != b)
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return true;
            }
            return false;
        }
        private bool CheckNgramsExist(int mode)
        {
            switch (mode)
            {
                case 0:
                case 1:
                case 2:
                    if (processor.GetLiteralNgrams().Count == 0)
                    {
                        MessageBox.Show("Буквені n-грами ще не пораховані.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    break;
                case 3:
                case 4:
                case 5:
                    if (processor.GetSymbolNgrams().Count == 0)
                    {
                        MessageBox.Show("Символьні n-грами ще не пораховані.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    break;
                case 6:
                case 7:
                    if (processor.GetWordsNgrams().Count == 0)
                    {
                        MessageBox.Show("Словесні n-грами ще не пораховані.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    break;
            }
            return true;
        }


        private bool CheckCharacterNgramLength(string ngram, int nPrime, int maxNValue, int mode)
        {
            if (mode == 0 || mode == 3)
            {
                if (nPrime > maxNValue)
                {
                    MessageBox.Show($"n' ({nPrime}) не може бути більше n ({maxNValue}), яке було встановлено в MainForm.",
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (ngram.Length > nPrime)
                {
                    MessageBox.Show($"Довжина n-грами ({ngram.Length}) перевищує n' ({nPrime}).",
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            if ((mode == 1 || mode == 2 || mode == 4 || mode == 5) && ngram.Length > maxNValue)
            {
                MessageBox.Show($"Довжина n-грами ({ngram.Length}) перевищує n ({maxNValue}), яке було встановлено в MainForm.",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool CheckWordNgram(string ngram, int nPrime, int maxNValue, int mode)
        {
            int wordCount = ngram.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

            if (nPrime > maxNValue)
            {
                MessageBox.Show($"n' ({nPrime}) не може бути більше n ({maxNValue}), яке було встановлено в MainForm.",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (mode == 6 && wordCount > nPrime)
            {
                MessageBox.Show($"Кількість слів ({wordCount}) перевищує n' ({nPrime}).",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateWordNgramInput(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                MessageBox.Show("Поле n-грами не може бути порожнім або містити лише пробіли.",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (rawInput.StartsWith(" ") || rawInput.EndsWith(" "))
            {
                MessageBox.Show("Введена n-грама має пробіли на початку або в кінці.",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (rawInput.Contains("  "))
            {
                MessageBox.Show("Введена n-грама містить подвійні пробіли всередині.",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string[] words = rawInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Regex validWord = new Regex(@"^[a-zA-Zа-яА-Я0-9]+$");

            foreach (string word in words)
            {
                if (!validWord.IsMatch(word))
                {
                    MessageBox.Show($"Некоректна n-грама: слово \"{word}\" містить недопустимі символи.\n" +
                        "Слова можуть містити лише літери та цифри.",
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            return true;
        }

    }
}
