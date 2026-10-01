using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Threading.Tasks;

namespace ThaiKeyboard
{
    public partial class MainWindow : Window
    {
        private bool _isShifted = false;
        private List<List<KeyModel>> _keyboardLayout = new List<List<KeyModel>>();
        private IntPtr _lastExternalWindow;
        
        private List<EmojiCategory> _allEmojis;

        // --- Win32 APIs ---
        const int GWL_EXSTYLE = -20;
        const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }

        public MainWindow()
        {
            InitializeComponent();
            InitializeEmojis();
            InitializeKeyboard();
        }

        private void InitializeEmojis()
        {
            _allEmojis = new List<EmojiCategory>
            {
                new EmojiCategory { CategoryName = "Frequently Used", Emojis = new List<string> { "👍", "😀", "😘", "😍", "😆", "😜", "😅", "😂", "😱" } },
                new EmojiCategory { CategoryName = "Smileys & People", Emojis = new List<string> { "😀", "😃", "😄", "😁", "😆", "😅", "😂", "🤣", "😊", "😇", "🙂", "🙃", "😉", "😌", "😍", "🥰", "😘", "😗", "😙", "😚", "😋", "😛", "😝", "😜", "🤪", "🤨", "🧐", "🤓", "😎", "🤩", "🥳", "😏", "😒", "😞", "😔", "😟", "😕", "🙁", "☹️", "😣", "😖", "😫", "😩", "🥺", "😢", "😭", "😤", "😠", "😡", "🤬", "🤯", "😳", "🥵", "🥶", "😱", "😨", "😰", "😥", "😓", "🤗", "🤔", "🤭", "🤫", "🤥", "😶", "😐", "😑", "😬", "🙄", "😯", "😦", "😧", "😮", "😲", "🥱", "😴", "🤤", "😪", "😵", "🤐", "🥴", "🤢", "🤮", "🤧", "😷", "🤒", "🤕", "🤑", "🤠", "😈", "👿", "👹", "👺", "🤡", "💩", "👻", "💀", "☠️", "👽", "👾", "🤖" } },
                new EmojiCategory { CategoryName = "Animals & Nature", Emojis = new List<string> { "🐶", "🐱", "🐭", "🐹", "🐰", "🦊", "🐻", "🐼", "🐨", "🐯", "🦁", "🐮", "🐷", "🐽", "🐸", "🐵", "🙈", "🙉", "🙊", "🐒", "🐔", "🐧", "🐦", "🐤", "🐣", "🐥", "🦆", "🦅", "🦉", "🦇", "🐺", "🐗", "🐴", "🦄", "🐝", "🐛", "🦋", "🐌", "🐞", "🐜", "🦟", "🦗", "🕷", "🕸", "🦂", "🐢", "🐍", "🦎", "🦖", "🦕", "🐙", "🦑", "🦐", "🦞", "🦀", "🐡", "🐠", "🐟", "🐬", "🐳", "🐋", "🦈", "🐊", "🐅", "🐆", "🦓", "🦍", "🦧", "🐘", "🦛", "🦏", "🐪", "🐫", "🦒", "🦘", "🐃", "🐂", "🐄", "🐎", "🐖", "🐏", "🐑", "🦙", "🐐", "🦌", "🐕", "🐩", "🦮", "🐕‍🦺", "🐈", "🐓", "🦃", "🦚", "🦜", "🦢", "🦩", "🕊", "🐇", "🦝", "🦨", "🦡", "🦦", "🦥", "🐁", "🐀", "🐿", "🦔", "🐾", "🐉", "🐲", "🌵", "🎄", "🌲", "🌳", "🌴", "🌱", "🌿", "☘", "🍀", "🎍", "🎋", "🍃", "🍂", "🍁", "🍄", "🌾", "💐", "🌷", "🌹", "🥀", "🌺", "🌸", "🌼", "🌻", "🌞", "🌝", "🌛", "🌜", "🌚", "🌕", "🌖", "🌗", "🌘", "🌑", "🌒", "🌓", "🌔", "🌙", "🌎", "🌍", "🌏", "🪐", "💫", "⭐", "🌟", "✨", "⚡", "☄", "💥", "🔥", "🌪", "🌈", "☀️", "🌤", "⛅", "🌥", "☁", "🌦", "🌧", "⛈", "🌩", "🌨", "❄", "☃", "⛄", "🌬", "💨", "💧", "💦", "☔", "☂", "🌊", "🌫" } }
            };
            EmojiItemsControl.ItemsSource = _allEmojis;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial state
            ToggleMode_Changed(null, null);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (DirectInputToggle.IsChecked == true)
            {
                IntPtr fw = GetForegroundWindow();
                if (fw != new WindowInteropHelper(this).Handle)
                {
                    _lastExternalWindow = fw;
                }
            }
        }

        private void ToggleMode_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            var helper = new WindowInteropHelper(this);
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);

            if (DirectInputToggle.IsChecked == true)
            {
                // Enable NOACTIVATE
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
                Topmost = true;
                TranslationGrid.Opacity = 0.5; // Dim translation area
                SourceTextBox.IsReadOnly = true;
            }
            else
            {
                // Disable NOACTIVATE
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle & ~WS_EX_NOACTIVATE);
                Topmost = false;
                TranslationGrid.Opacity = 1.0;
                SourceTextBox.IsReadOnly = false;
            }
        }

        private void InitializeKeyboard()
        {
            var row1 = new List<KeyModel>
            {
                new KeyModel("_", "%", "Oem3"), new KeyModel("ๅ", "+", "D1"), new KeyModel("/", "๑", "D2"),
                new KeyModel("-", "๒", "D3"), new KeyModel("ภ", "๓", "D4"), new KeyModel("ถ", "๔", "D5"),
                new KeyModel("ุ", "ู", "D6"), new KeyModel("ึ", "฿", "D7"), new KeyModel("ค", "๕", "D8"),
                new KeyModel("ต", "๖", "D9"), new KeyModel("จ", "๗", "D0"), new KeyModel("ข", "๘", "OemMinus"),
                new KeyModel("ช", "๙", "OemPlus"), new KeyModel("⌫", "⌫", "Backspace", width: 80)
            };
            var row2 = new List<KeyModel>
            {
                new KeyModel("Tab", "Tab", "Tab", width: 80), new KeyModel("ๆ", "๐", "Q"), new KeyModel("ไ", "\"", "W"),
                new KeyModel("ำ", "ฎ", "E"), new KeyModel("พ", "ฑ", "R"), new KeyModel("ะ", "ธ", "T"),
                new KeyModel("ั", "ํ", "Y"), new KeyModel("ี", "๊", "U"), new KeyModel("ร", "ณ", "I"),
                new KeyModel("น", "ฯ", "O"), new KeyModel("ย", "ญ", "P"), new KeyModel("บ", "ฐ", "OemOpenBrackets"),
                new KeyModel("ล", ",", "OemCloseBrackets"), new KeyModel("ฃ", "ฅ", "OemPipe")
            };
            var row3 = new List<KeyModel>
            {
                new KeyModel("Caps", "Caps", "CapsLock", width: 100), new KeyModel("ฟ", "ฤ", "A"), new KeyModel("ห", "ฆ", "S"),
                new KeyModel("ก", "ฏ", "D"), new KeyModel("ด", "โ", "F"), new KeyModel("เ", "ฌ", "G"),
                new KeyModel("้", "็", "H"), new KeyModel("่", "ษ", "J"), new KeyModel("า", "ศ", "K"),
                new KeyModel("ส", "ซ", "L"), new KeyModel("ว", ".", "OemSemicolon"), new KeyModel("ง", "(", "OemQuotes"),
                new KeyModel("↵", "↵", "Enter", width: 100)
            };
            var row4 = new List<KeyModel>
            {
                new KeyModel("⇧", "⇧", "Shift", width: 120), new KeyModel("ผ", "(", "Z"), new KeyModel("ป", ")", "X"),
                new KeyModel("แ", "ฉ", "C"), new KeyModel("อ", "ฮ", "V"), new KeyModel("ิ", "ฺ", "B"),
                new KeyModel("ื", "์", "N"), new KeyModel("ท", "?", "M"), new KeyModel("ม", "ฒ", "OemComma"),
                new KeyModel("ใ", "ฬ", "OemPeriod"), new KeyModel("ฝ", "ฦ", "OemQuestion"), new KeyModel("⇧", "⇧", "Shift", width: 120)
            };
            var row5 = new List<KeyModel>
            {
                new KeyModel("😂😘", "😂😘", "Emoji", width: 80),
                new KeyModel("alt", "alt", "Alt", width: 60),
                new KeyModel("Space", "Space", "Space", width: 340),
                new KeyModel("alt", "alt", "Alt", width: 60)
            };
            _keyboardLayout.Add(row1); _keyboardLayout.Add(row2); _keyboardLayout.Add(row3);
            _keyboardLayout.Add(row4); _keyboardLayout.Add(row5);
            UpdateKeyboardDisplay();
        }

        private void UpdateKeyboardDisplay()
        {
            foreach (var row in _keyboardLayout)
                foreach (var key in row)
                    key.IsShifted = _isShifted;
            KeyboardItemsControl.ItemsSource = null;
            KeyboardItemsControl.ItemsSource = _keyboardLayout;
        }

        private void KeyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is KeyModel keyModel)
            {
                if (keyModel.KeyName == "Emoji")
                {
                    KeyboardItemsControl.Visibility = Visibility.Collapsed;
                    EmojiGrid.Visibility = Visibility.Visible;
                    return;
                }
                
                if (keyModel.KeyName == "Alt") return;

                if (DirectInputToggle.IsChecked == true)
                {
                    HandleExternalInput(keyModel);
                }
                else
                {
                    HandleInternalInput(keyModel);
                }
            }
        }

        private void BackToKeyboard_Click(object sender, RoutedEventArgs e)
        {
            KeyboardItemsControl.Visibility = Visibility.Visible;
            EmojiGrid.Visibility = Visibility.Collapsed;
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is string emoji)
            {
                if (DirectInputToggle.IsChecked == true)
                {
                    if (_lastExternalWindow != IntPtr.Zero && GetForegroundWindow() != _lastExternalWindow)
                    {
                        SetForegroundWindow(_lastExternalWindow);
                        System.Threading.Thread.Sleep(20);
                    }
                    SendUnicodeString(emoji);
                }
                else
                {
                    InsertInternalText(emoji);
                    SourceTextBox.Focus();
                }
            }
        }

        private void HandleInternalInput(KeyModel key)
        {
            if (key.KeyName == "Shift")
            {
                _isShifted = !_isShifted;
                UpdateKeyboardDisplay();
                return;
            }

            int selectionStart = SourceTextBox.SelectionStart;
            switch (key.KeyName)
            {
                case "Backspace":
                    if (selectionStart > 0 && SourceTextBox.SelectionLength == 0)
                    {
                        SourceTextBox.Text = SourceTextBox.Text.Remove(selectionStart - 1, 1);
                        SourceTextBox.SelectionStart = selectionStart - 1;
                    }
                    else if (SourceTextBox.SelectionLength > 0)
                    {
                        int start = SourceTextBox.SelectionStart;
                        SourceTextBox.Text = SourceTextBox.Text.Remove(start, SourceTextBox.SelectionLength);
                        SourceTextBox.SelectionStart = start;
                    }
                    break;
                case "Enter":
                    InsertInternalText("\n");
                    break;
                case "Space":
                    InsertInternalText(" ");
                    break;
                case "Tab":
                    InsertInternalText("\t");
                    break;
                case "CapsLock":
                    break;
                default:
                    InsertInternalText(key.DisplayText);
                    if (_isShifted)
                    {
                        _isShifted = false;
                        UpdateKeyboardDisplay();
                    }
                    break;
            }
            SourceTextBox.Focus();
        }

        private void InsertInternalText(string text)
        {
            int selectionStart = SourceTextBox.SelectionStart;
            if (SourceTextBox.SelectionLength > 0)
            {
                SourceTextBox.Text = SourceTextBox.Text.Remove(selectionStart, SourceTextBox.SelectionLength);
            }
            SourceTextBox.Text = SourceTextBox.Text.Insert(selectionStart, text);
            SourceTextBox.SelectionStart = selectionStart + text.Length;
        }

        private void HandleExternalInput(KeyModel key)
        {
            if (key.KeyName == "Shift")
            {
                _isShifted = !_isShifted;
                UpdateKeyboardDisplay();
                return;
            }

            // Restore focus to last window if needed (usually NOACTIVATE handles this, but just in case)
            if (_lastExternalWindow != IntPtr.Zero && GetForegroundWindow() != _lastExternalWindow)
            {
                SetForegroundWindow(_lastExternalWindow);
                System.Threading.Thread.Sleep(20);
            }

            switch (key.KeyName)
            {
                case "Backspace": SendVk(0x08); break;
                case "Enter": SendVk(0x0D); break;
                case "Space": SendVk(0x20); break;
                case "Tab": SendVk(0x09); break;
                case "CapsLock": break;
                default:
                    SendUnicodeString(key.DisplayText);
                    if (_isShifted)
                    {
                        _isShifted = false;
                        UpdateKeyboardDisplay();
                    }
                    break;
            }
        }

        private void SendVk(ushort vk)
        {
            INPUT down = new INPUT { type = 1 }; down.u.ki.wVk = vk;
            INPUT up = new INPUT { type = 1 }; up.u.ki.wVk = vk; up.u.ki.dwFlags = 0x0002;
            SendInput(2, new INPUT[] { down, up }, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SendUnicodeString(string text)
        {
            List<INPUT> inputs = new List<INPUT>();
            foreach (char c in text)
            {
                INPUT down = new INPUT { type = 1 };
                down.u.ki.wScan = c;
                down.u.ki.dwFlags = 0x0004; // KEYEVENTF_UNICODE
                inputs.Add(down);

                INPUT up = new INPUT { type = 1 };
                up.u.ki.wScan = c;
                up.u.ki.dwFlags = 0x0004 | 0x0002; // KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
                inputs.Add(up);
            }
            if (inputs.Count > 0)
            {
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
            }
        }

        private void CopyTarget_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TargetTextBox.Text))
            {
                Clipboard.SetText(TargetTextBox.Text);
            }
        }

        private void ClearText_Click(object sender, RoutedEventArgs e)
        {
            SourceTextBox.Clear();
            TargetTextBox.Clear();
        }

        private void SwapLanguage_Click(object sender, RoutedEventArgs e)
        {
            int sourceIndex = SourceLanguageComboBox.SelectedIndex;
            int targetIndex = TargetLanguageComboBox.SelectedIndex;
            
            SourceLanguageComboBox.SelectedIndex = targetIndex;
            TargetLanguageComboBox.SelectedIndex = sourceIndex;

            string sourceText = SourceTextBox.Text;
            string targetText = TargetTextBox.Text;

            SourceTextBox.Text = targetText;
            TargetTextBox.Text = sourceText;
        }

        private async void Translate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SourceTextBox.Text)) return;
            
            string sourceLang = (SourceLanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "th";
            string targetLang = (TargetLanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "id";

            TargetTextBox.Text = "Translating...";
            try
            {
                using var client = new HttpClient();
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&dt=rm&q={Uri.EscapeDataString(SourceTextBox.Text)}";
                
                string json = await client.GetStringAsync(url);
                var root = JsonNode.Parse(json) as JsonArray;
                
                string translatedText = "";
                string romanizedText = "";
                
                if (root != null && root.Count > 0 && root[0] is JsonArray dataArray)
                {
                    foreach (var item in dataArray)
                    {
                        if (item is JsonArray segmentData)
                        {
                            if (segmentData.Count > 0 && segmentData[0] != null)
                            {
                                try { translatedText += (string)segmentData[0]; } catch {}
                            }
                            else if (segmentData.Count > 2 && segmentData[0] == null && segmentData[2] != null)
                            {
                                try { romanizedText += (string)segmentData[2]; } catch {}
                            }
                        }
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(romanizedText))
                {
                    TargetTextBox.Text = $"{translatedText}\n\nSubtitle (Pronunciation):\n{romanizedText}";
                }
                else
                {
                    TargetTextBox.Text = translatedText;
                }
            }
            catch (Exception ex)
            {
                TargetTextBox.Text = $"[Offline/Error] Could not connect to translation API.\n{ex.Message}";
            }
        }

        private System.Threading.CancellationTokenSource _predictionCts;
        private string _currentWordToReplace = "";
        private int _currentWordStartIndex = -1;

        private async void SourceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string sourceLang = (SourceLanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (sourceLang != "th")
            {
                PredictionPopup.IsOpen = false;
                return;
            }

            int caretIndex = SourceTextBox.SelectionStart;
            string text = SourceTextBox.Text;
            if (caretIndex <= 0 || caretIndex > text.Length)
            {
                PredictionPopup.IsOpen = false;
                return;
            }

            int start = caretIndex - 1;
            while (start >= 0 && !char.IsWhiteSpace(text[start]))
            {
                start--;
            }
            start++;

            if (start >= caretIndex)
            {
                PredictionPopup.IsOpen = false;
                return;
            }

            string currentWord = text.Substring(start, caretIndex - start);
            _currentWordToReplace = currentWord;
            _currentWordStartIndex = start;

            _predictionCts?.Cancel();
            _predictionCts = new System.Threading.CancellationTokenSource();
            var token = _predictionCts.Token;

            try
            {
                await Task.Delay(200, token);
                if (token.IsCancellationRequested) return;

                bool isLatin = true;
                foreach (char c in currentWord)
                {
                    if (c >= 0x0E00 && c <= 0x0E7F) // Thai Unicode block
                    {
                        isLatin = false;
                        break;
                    }
                }

                using var client = new HttpClient();
                string json = "";
                var list = new List<string>();

                if (isLatin)
                {
                    string url = $"https://inputtools.google.com/request?text={currentWord}&itc=th-t-i0-und&num=6&cp=0&cs=1&ie=utf-8&oe=utf-8";
                    json = await client.GetStringAsync(url, token);
                    var root = JsonNode.Parse(json) as JsonArray;
                    if (root != null && root.Count > 1 && root[0].ToString() == "SUCCESS" && root[1] is JsonArray dataArr && dataArr.Count > 0)
                    {
                        if (dataArr[0] is JsonArray wordData && wordData.Count > 1 && wordData[1] is JsonArray suggestions)
                        {
                            foreach (var s in suggestions)
                            {
                                list.Add(s.ToString());
                            }
                        }
                    }
                }
                else
                {
                    string url = $"http://suggestqueries.google.com/complete/search?client=firefox&q={Uri.EscapeDataString(currentWord)}&hl=th";
                    json = await client.GetStringAsync(url, token);
                    var root = JsonNode.Parse(json) as JsonArray;
                    if (root != null && root.Count > 1 && root[1] is JsonArray suggestions)
                    {
                        foreach (var s in suggestions)
                        {
                            list.Add(s.ToString());
                        }
                    }
                }

                if (list.Count > 0)
                {
                    PredictionListBox.ItemsSource = list;
                    PredictionPopup.IsOpen = true;
                }
                else
                {
                    PredictionPopup.IsOpen = false;
                }
            }
            catch { }
        }

        private void SourceTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (PredictionPopup.IsOpen && PredictionListBox.Items.Count > 0)
            {
                if (e.Key == System.Windows.Input.Key.Down)
                {
                    PredictionListBox.Focus();
                    PredictionListBox.SelectedIndex = 0;
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Space || e.Key == System.Windows.Input.Key.Enter)
                {
                    string selectedWord = PredictionListBox.Items[0] as string;
                    InsertPrediction(selectedWord);
                    if (e.Key == System.Windows.Input.Key.Enter)
                    {
                        e.Handled = true;
                    }
                }
                else if (e.Key == System.Windows.Input.Key.Escape)
                {
                    PredictionPopup.IsOpen = false;
                    e.Handled = true;
                }
            }
        }

        private void PredictionListBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space || e.Key == System.Windows.Input.Key.Enter)
            {
                if (PredictionListBox.SelectedItem is string selectedWord)
                {
                    InsertPrediction(selectedWord);
                    if (e.Key == System.Windows.Input.Key.Space)
                    {
                        InsertInternalText(" ");
                    }
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                PredictionPopup.IsOpen = false;
                SourceTextBox.Focus();
                e.Handled = true;
            }
        }

        private void PredictionItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is string selectedWord)
            {
                InsertPrediction(selectedWord);
            }
        }

        private void InsertPrediction(string selectedWord)
        {
            if (_currentWordStartIndex >= 0 && _currentWordStartIndex <= SourceTextBox.Text.Length)
            {
                string text = SourceTextBox.Text;
                int lengthToReplace = _currentWordToReplace.Length;
                
                SourceTextBox.TextChanged -= SourceTextBox_TextChanged;
                string newText = text.Remove(_currentWordStartIndex, lengthToReplace).Insert(_currentWordStartIndex, selectedWord);
                SourceTextBox.Text = newText;
                SourceTextBox.SelectionStart = _currentWordStartIndex + selectedWord.Length;
                SourceTextBox.TextChanged += SourceTextBox_TextChanged;
            }
            PredictionPopup.IsOpen = false;
            SourceTextBox.Focus();
        }
    }

    public class KeyModel : INotifyPropertyChanged
    {
        public string NormalChar { get; }
        public string ShiftChar { get; }
        public string KeyName { get; }
        public int Width { get; }

        private bool _isShifted;
        public bool IsShifted
        {
            get => _isShifted;
            set
            {
                if (_isShifted != value)
                {
                    _isShifted = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
                }
            }
        }

        public string DisplayText => IsShifted ? ShiftChar : NormalChar;

        public KeyModel(string normalChar, string shiftChar, string keyName, int width = 50)
        {
            NormalChar = normalChar;
            ShiftChar = shiftChar;
            KeyName = keyName;
            Width = width;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class EmojiCategory
    {
        public string CategoryName { get; set; }
        public List<string> Emojis { get; set; }
    }
}
