using Emgu.CV.Structure;
using Emgu.CV;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using multimodalInputs.Data;
using Tesseract;
using Toolbelt.Blazor.SpeechRecognition;
using BlazorApp1.Data;

namespace BlazorApp1.Components.Pages
{
    public partial class ImageCreation
    {
        public string inputText { get; set; }
        public string? textImageUrl { get; set; } //Referenz zum Hintergrundbild
        public int textImageWidth = 0;
        public int textImageHeight = 0;
        public ElementReference _canvas;
        public string color = "rgba(0,0,0,1)";
        public SixLabors.ImageSharp.Image<Rgba32> textImage { get; set; }
        SixLabors.Fonts.FontFamily fontFamily;
        FontCollection fontCollection = new();

        public int userPromptCounter = 0;
        public string promptAnswer = "";
        public List<String> promptAnswers = new List<string>();
        public string userPrompt = "";
        public List<String> userPrompts = new List<string>();
        public String[] markedTexts = new String[5];
        //markedTexts[0]= gelb (=>intent.custom), markedTexts[1]=rot formulate (word),
        //markedTexts[2]= orange ==> generate (start), markedTexts[3] = purple ==> formulate (context), markedTexts[4] = cyan ==> generate (end)
        public bool massMark;
        public intent userIntent;
        public string processedIntet = "";
        public List<String> textPrompts = new List<string>();
        public List<String> voicePrompts = new List<string>();
        public List<String> textPromptAnswers = new List<string>();



        string speechButtonText = "Start Recording";
        bool speechactivated = false;
        bool speechWasUsed = false;
        private SpeechRecognitionResult[] _results = Array.Empty<SpeechRecognitionResult>();

        bool speechButtonHidden = true;
        bool ChatInputHidden = false;
        bool viewResultHidden = true;
        string switchText = "Switch to Voice Input";
        private IOCrService OCrService;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JsRuntime.InvokeAsync<string>("setSignature", _canvas, color, null);
            }
        }
        protected override async Task OnInitializedAsync()
        {
            //Initialisiere die Spracheingabe
            this.SpeechRecognition.Lang = "en-US";
            this.SpeechRecognition.InterimResults = true;
            this.SpeechRecognition.Continuous = true;
            this.SpeechRecognition.Result += OnSpeechRecognized;
            //Füge arial als font hinzu
            fontFamily = fontCollection.Add("Data/arial.ttf");
            SixLabors.Fonts.Font font = fontFamily.CreateFont(12, SixLabors.Fonts.FontStyle.Italic);
            //Speichere den Text aus dem Editor
            string textInput = StringService.EditorContent;
            inputText = textInput;
            //Erstelle aus dem textInput ein Bild
            SixLabors.ImageSharp.Image<Rgba32> img = DrawTextToImage(textInput, font, 12);
            textImage = img;
            //Speichere die Höhe und breite des Bildes um das signaturePad anzupassen
            textImageHeight = img.Height;
            textImageWidth = img.Width;
            //Konvertiere das Bild zu einere dataUrl um es im signaturePad darstellen zu können
            IImageEncoder format = new PngEncoder();
            textImageUrl = "data:image/png;base64," + Convert.ToBase64String(ConvertImageToByteArray(img, format));
            //Lade die Komponenten neu
            this.userIntent = intent.custom;
            massMark = false;
            await InvokeAsync(StateHasChanged);
        }

        public void back()
        {
            //Navigate to Editor Page
            NavManager.NavigateTo($"/start/Back");
        }
        public void copyToClipboard(string text)
        {
            //rufe js function copyToClipboard auf
            JsRuntime.InvokeVoidAsync("copyToClipboard", text);
        }
        public async Task processInput()
        {
            //Lade Markierungen auf dem Text
            byte[] imageData = await JsRuntime.InvokeAsync<byte[]>("getImageData", _canvas);
            Image<Rgba32> markerImage = LoadImageFromByteArray(imageData);
            IImageEncoder format = new PngEncoder();
            Image<Rgba32> backgroundTextImage = LoadImageFromByteArray(ConvertImageToByteArray(textImage, format));
            //Kombiniere das Bild mit den Markierungen mit dem Bild mit Text
            var result = CombineImages(backgroundTextImage, markerImage);
            //finde markierte Bereiche
            processImage(result);

            switch (userIntent)
            {
                //unterscheide zwischen verschiedenen Intents
                case intent.custom:

                    string prompt;
                    if (speechWasUsed && markedTexts[0] == "")
                    {
                        prompt = userPrompt + "\n Full text:" + inputText;
                        string promptAnswer = await SendPrompt(prompt);
                        promptAnswers.Add(promptAnswer);
                        userPrompts.Add(userPrompt);
                        userPromptCounter++;
                        break;

                    }
                    //finde Intent der Chateingabe heraus
                    string intentPrompt = "Extract and formulate the user's intent as a clear instruction in one sentence. The user is currently using a text editor app. Here are some examples of instructions:\n" +
                                          "- 'Translate the following text to Italian.'\n" +
                                          "- 'Summarize the  text'\n" +
                                          "- 'Replace word x with word y'\n" +
                                          "Now, based on the following input provided by the user, generate a similar instruction in one sentence:\n\n" +
                                          userPrompt;
                    string intentAnswer = await SendPrompt(intentPrompt);
                    if (markedTexts[0] == "")
                    {
                        prompt= intentAnswer+"\n Full text: " + inputText;
                        string promptAnswer = await SendPrompt(prompt);
                        promptAnswers.Add(promptAnswer);
                        userPrompts.Add(intentAnswer);
                        userPromptCounter++;
                        userPrompt = "";
                        break;
                    }
                    //unterscheide zwischen Stiftmodus
                    if (massMark)
                    {
                        //wende Intent an
                        prompt = "A user marked words of a paragraph of the text. Identify which paragraph the user intended to mark based on the marked words. Then execute this instruction: " + intentAnswer + ". Your answer shall only contain the end result after applying the instruction. The marked words might be fuzzy, so do your best to match them to the correct paragraph.\n\n" +
                                        "Marked Words:\n" + markedTexts[0] + "\n\n" +
                                        "Full Text:\n" + inputText;
                        string promptAnswer = await SendPrompt(prompt);
                        //füge Antwort der Antwortliste hinzu
                        promptAnswers.Add(promptAnswer);
                        //füge Prompt der Promptliste hinzu
                        userPrompts.Add(userPrompt);
                        //inkrementiere Laufvariable für Chat
                        userPromptCounter++;
                    }
                    else
                    {
                        //wende Intent an
                        prompt = "A user using a texteditor submitted the following Text : " + markedTexts[0] + "with the following instruction: " + intentAnswer ;
                        string promptAnswer = await SendPrompt(prompt);
                        promptAnswers.Add(promptAnswer);
                        userPrompts.Add(userPrompt);
                        userPromptCounter++;
                    }
                    break;
                case intent.formulate:
                    //Wende Formulierungsprompt an
                    string formatPrompt = "Return at least 3 word suggestions for the word "+ markedTexts[1]+" in the following context: '"+ markedTexts[3] + "\n Your answer shall only contain the three suggestions.";
                    //Sende Prompt an GPT
                    string formatPromptAnswer = await SendPrompt(formatPrompt);
                    //füge Antwort der Antwortliste hinzu
                    promptAnswers.Add(formatPromptAnswer);
                    //füge Prompt der Promptliste hinzu
                    userPrompts.Add(formatPrompt);
                    //inkrementiere Laufvariable für Chat
                    userPromptCounter++;
                    break;
                case intent.generate:
                    //Wende Generierungsprompt an
                    string generatePrompt = "";
                    if (userPrompt != null)
                        generatePrompt = "Generate a text with the following Instruction “" + userPrompt + "” which is supposed to fit in the follwing text: „" + inputText + "“ between „" + markedTexts[2] + "“ and „" + markedTexts[4] + "“ and return the whole text";
                    else generatePrompt = "Generate a text which is supposed to fit in the follwing text: „" + inputText + "“ between „" + markedTexts[2] + "“ and „" + markedTexts[4] + "“ and return only the whole text";
                    //Sende Prompt an GPT
                    string generatePromptAnswer = await SendPrompt(generatePrompt);
                    //füge Antwort der Antwortliste hinzu
                    promptAnswers.Add(generatePromptAnswer);
                    //füge Prompt der Promptliste hinzu
                    userPrompts.Add("Generate");
                    //inkrementiere Laufvariable für Chat
                    userPromptCounter++;
                    break;
                default:
                    Console.WriteLine("something went wrong");
                    break;
            }
           
            viewResultHidden = false;
            userPrompt = "";
        }
        public async void speech()
        {
            //Wird nachdem Voice input Button aktiviert
            if (speechactivated)
            {
                //wenn Spracheingabe aktiviert war stoppe diese 
                await this.SpeechRecognition.StopAsync();
                //und speichere die Eingabe
                string speech = "";
                foreach (var words in this._results)
                {
                    speech += words.Items[0].Transcript;
                }
                voicePrompts.Add(speech);
                //Komponenten werden zurückgesetzt, damit erneute Spracheingabe möglich ist
                speechactivated = false;
                speechWasUsed = true;
                speechButtonText = "Restart Recording";
                userPrompt = speech;
                viewResultHidden = false;
                //Die Komponenten werden neu geladen
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                //Wenn die Spracheingabe nicht aktiviert war aktiviere diese
                await this.SpeechRecognition.StartAsync();
                //Ändere die Komponenten, damit der User die Spracheingabe stoppen kann
                speechactivated = true;
                speechButtonText = "Stop Recording";
                //Die Komponenten werden neu geladen
                await InvokeAsync(StateHasChanged);
            }
        }
        public async void switchTo()
        {
            //Wechsel zwischen Sprach- und Texteingabe
            if (speechButtonHidden)
            {
                //Wenn Spracheingabe deaktiviert war, aktiviere/deaktiviere die entsprechenden Buttons
                speechButtonHidden = false;
                ChatInputHidden = true;
                switchText = "Switch to Text Input";
                //Die Komponenten werden neu geladen
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                //Wenn Spracheingabe aktiviert war, aktiviere/deaktiviere die entsprechenden Buttons
                speechButtonHidden = true;
                ChatInputHidden = false;
                switchText = "Switch to Voice Input";
                //Die Komponenten werden neu geladen
                await InvokeAsync(StateHasChanged);
            }
        }
        public void viewResult()
        {
            StringService.Answer = promptAnswers.ToArray()[promptAnswers.ToArray().Length - 1];
            //File.WriteAllText("result.txt", promptAnswers.ToArray()[promptAnswers.ToArray().Length - 1]);
            //Navigate to Editor Page
            NavManager.NavigateTo($"/start/Result");
        }
        private Image<Gray, byte> applyMask(Image<Gray, byte> mask)
        {
            try
            {
                //lade Bild vom Text
                Image<Hsv, byte> hsvImage = ConvertToEmguCvImage(textImage);
                Image<Bgr, byte> text = hsvImage.Convert<Bgr, byte>();
                Image<Gray, byte> invertedMask = mask.Not();
                //invertiere Maske von schwarz zu weiss
                Image<Bgr, byte> invertedMaskBgr = invertedMask.Convert<Bgr, byte>();
                //erstelle neues Bild und kombiniere Maske mit Bild vom Text
                Image<Bgr, byte> whiteMaskedTextImageY = new Image<Bgr, byte>(text.Size);
                CvInvoke.BitwiseOr(text, invertedMaskBgr, whiteMaskedTextImageY, null);
                //konvertiere Bild zu grayscale
                Image<Gray, byte> grayMaskedTextImage = whiteMaskedTextImageY.Convert<Gray, byte>();

                return grayMaskedTextImage;
            }
            catch (Exception ex)
            {
                Console.WriteLine("an error occured in applymask : " + ex.Message);
                return null;
            }
        }
        
        public Image<Rgba32> CombineImages(Image<Rgba32> baseImage, Image<Rgba32> overlayImage)
        {
            // Klone das Bild, das im Hintergrund stehen soll
            Image<Rgba32> combinedImage = baseImage.Clone();

            // Passe das geklonte Bild an das Bild das darübergelegt werden soll an falls nötig
            if (overlayImage.Width != baseImage.Width || overlayImage.Height != baseImage.Height)
            {
                overlayImage.Mutate(x => x.Resize(baseImage.Width, baseImage.Height));
            }

            // Lege das overlayImage über das geklonte und angepasste Bild
            combinedImage.Mutate(x => x.DrawImage(overlayImage, 1f));

            return combinedImage;
        }
        public byte[] ConvertImageToByteArray(SixLabors.ImageSharp.Image image, IImageEncoder format)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, format);
                return stream.ToArray();
            }
        }
        public static Image<Rgba32> DataUrlToImage(string dataUrl)
        {
            // Trenne den Base64-Teil der Data-URL vom Präfix
            var base64Data = dataUrl.Split(',')[1];

            // Dekodiere den Base64-String in einen Byte-Array
            var imageBytes = Convert.FromBase64String(base64Data);

            // Lade das Byte-Array als Image mit ImageSharp
            using (var ms = new MemoryStream(imageBytes))
            {
                var image = SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
                return image;
            }
        }
        public void Dispose()
        {
            this.SpeechRecognition.Result -= OnSpeechRecognized;
        }

        public SixLabors.ImageSharp.Image<Rgba32> DrawTextToImage(string text, SixLabors.Fonts.Font font, int fontSize)
        {
            
            if (text.Length <= 1) {
                var whiteImage = new Image<Rgba32>(200, 200);
                whiteImage.Mutate(ctx => ctx.Fill(Color.White));
                return whiteImage;
            }
            
            
            //formatiere Textinput string
            string newText = "";
            int i = 0;
            foreach (char a in text)
            {
                if (a == '\n')
                {
                    newText += "\n";
                    i = 0;
                    continue;
                }
                if (i > 60 && (a == ' '))
                {
                    newText += "\n";
                    i = 0;
                }
                else
                {
                    newText += a;
                    i += 1;
                }
            }


            // Vermesse den Text
            var textSize = TextMeasurer.MeasureSize(newText, new TextOptions(font));

            using (var img = new Image<Rgba32>((int)textSize.Width, (int)textSize.Height + 10))
            {
                //Erzeuge neues Bild mit der gemessenen Höhe und Breite
                var image = new Image<Rgba32>(img.Width, img.Height);
                // Male den Text auf das erzeugte Bild
                image.Mutate(ctx => ctx
                    .Fill(SixLabors.ImageSharp.Color.White) // White background
                    .DrawText(newText, font, SixLabors.ImageSharp.Color.Black, new SixLabors.ImageSharp.PointF(0, 0))); // Black text at position (0, 0)

                return image;
            }
        }
        public Image<Rgba32> LoadImageFromByteArray(byte[] imageData)
        {
            using (MemoryStream stream = new MemoryStream(imageData))
            {
                return SixLabors.ImageSharp.Image.Load<Rgba32>(stream);
            }
        }
        private async Task OnClickStart()
        {
            await this.SpeechRecognition.StartAsync();
        }
        private void OnSpeechRecognized(object sender, SpeechRecognitionEventArgs args)
        {
            this._results = args.Results.Skip(args.ResultIndex).ToArray();
            this.StateHasChanged();
        }

        private void processImage(Image<Rgba32> image)
        {

            //konvertiere zu HSV Format
            Image<Hsv, byte> hsvImage = ConvertToEmguCvImage(image);

            //definiere Bounds fuer die Farben der Markierungen
            Hsv yellowLowerBound = new Hsv(20, 100, 100);
            Hsv yellowUpperBound = new Hsv(30, 255, 255);


            Hsv redLowerBound = new Hsv(0, 100, 100);
            Hsv redUpperBound = new Hsv(10, 255, 255);

            Hsv orangeLowerBound = new Hsv(10, 100, 100);
            Hsv orangeUpperBound = new Hsv(20, 255, 255);

            Hsv purpleLowerBound = new Hsv(140, 100, 100);
            Hsv purpleUpperBound = new Hsv(160, 255, 255);

            Hsv cyanLowerBound = new Hsv(85, 100, 100);
            Hsv cyanUpperBound = new Hsv(100, 255, 255);

            //falls gelb und großer Stift vervollständige die Markierung
            Image<Gray, byte> yellowMask = hsvImage.InRange(yellowLowerBound, yellowUpperBound);
            if (userIntent == intent.custom && massMark)
            {
                for (int y = 0; y < hsvImage.Height; y++)
                {
                    bool isYellowRow = false;

                    //suche gelbe Pixel
                    for (int x = 0; x < hsvImage.Width; x++)
                    {
                        if (yellowMask.Data[y, x, 0] > 0)
                        {
                            isYellowRow = true;
                            break;
                        }
                    }

                    // falls gelber Pixel gefunden wurde ersetze alle Pixel in der Reihe durch gelb
                    if (isYellowRow)
                    {
                        for (int x = 0; x < hsvImage.Width; x++)
                        {
                            hsvImage[y, x] = new Hsv(25, 255, 255); // setze Farbe auf gelb
                        }
                    }
                }

            }

            //erstelle Masken fuer die markierten Bereiche
            Image<Gray, byte> maskYellow = hsvImage.InRange(yellowLowerBound, yellowUpperBound);
            Image<Gray, byte> maskRed = hsvImage.InRange(redLowerBound, redUpperBound);
            Image<Gray, byte> maskOrange = hsvImage.InRange(orangeLowerBound, orangeUpperBound);
            Image<Gray, byte> maskPurple = hsvImage.InRange(purpleLowerBound, purpleUpperBound);
            Image<Gray, byte> maskCyan = hsvImage.InRange(cyanLowerBound, cyanUpperBound);

            //wende Masken an und uebergebe das Bild an Tesseract um String zu bekommen
            Image<Gray, byte> maskedYellowImage = applyMask(maskYellow);
            markedTexts[0] = OcrService.performOcr(maskedYellowImage);

            Image<Gray, byte> maskedRedImage = applyMask(maskRed);
            markedTexts[1] = OcrService.performOcr(maskedRedImage);

            Image<Gray, byte> maskedOrangeImage = applyMask(maskOrange);
            markedTexts[2] = OcrService.performOcr(maskedOrangeImage);

            Image<Gray, byte> maskedPurpleImage = applyMask(maskPurple);
            markedTexts[3] = OcrService.performOcr(maskedPurpleImage);

            Image<Gray, byte> maskedCyanImage = applyMask(maskCyan);
            markedTexts[4] = OcrService.performOcr(maskedCyanImage);

            
        }

        public void SelectFormulate(string mark)
        {
            //setze Intent und schliesse das Modal
            userIntent = intent.formulate;
            JsRuntime.InvokeVoidAsync("closeModal", "#formulateButtonModal");
        }
        public void SelectGenerate()
        {
            //setze Intent und schliesse das Modal
            userIntent = intent.generate;
            processedIntet = ("The user is using a text editor and wants to generate Text");
            JsRuntime.InvokeVoidAsync("closeModal", "#generateButtonModal");
        }
        public void SelectMass(string mode)
        {
            this.userIntent = intent.custom;
            //setze Modus auf Massenmarkierung
            if (mode == "big")
            {
                massMark = true;
            }
            else
            {
                massMark = false;
            }
            JsRuntime.InvokeVoidAsync("closeModal", "#markButtonModal");
        }

        public void SaveImageToFile(Image<Rgba32> image, string filePath)
        {
            using (var stream = File.OpenWrite(filePath))
            {
                image.SaveAsPng(stream);
            }
        }
        private async Task<string> SendPrompt(string prompt)
        {
            try
            {
                //sende Prompt und warte auf Antwort
                string answer = await LLMService.GetResponseFromChatGPT(prompt);
                Console.WriteLine(answer);
                return answer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "";
            }
        }
        static Image<Hsv, byte> ConvertToEmguCvImage(Image<Rgba32> imageSharpImage)
        {
            //Vermesse das Eingabebild
            int width = imageSharpImage.Width;
            int height = imageSharpImage.Height;

            // Erstelle ein Emgu.Cv Image mit derselben Höhe und Breite des Eingabebildes
            Image<Hsv, byte> emguCvImage = new Image<Hsv, byte>(width, height);

            // Iteriere durch die Pixelreihe
            imageSharpImage.ProcessPixelRows(accessor =>
            {
                //Iteriere durch alle Pixel der Reihe
                for (int y = 0; y < height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < width; x++)
                    {
                        //Berechne für jeden rgba Wert des Eingabebildes den entsprechenden Hsv Wert
                        var pixel = row[x];
                        var hsvPixel = Rgba32ToHsv(pixel);
                        //Speichere den Hsv wert im erzeugten Bild an der entsprechenden Stelle ein
                        emguCvImage[y, x] = hsvPixel;
                    }
                }
            });

            return emguCvImage;
        }

        static Hsv Rgba32ToHsv(Rgba32 pixel)
        {
            // Konvertiere rgb zu Hsv
            float r = pixel.R / 255f;
            float g = pixel.G / 255f;
            float b = pixel.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            float h = 0;
            float s = max == 0 ? 0 : delta / max;
            float v = max;

            if (delta != 0)
            {
                if (max == r)
                {
                    h = 60 * (((g - b) / delta) % 6);
                }
                else if (max == g)
                {
                    h = 60 * (((b - r) / delta) + 2);
                }
                else if (max == b)
                {
                    h = 60 * (((r - g) / delta) + 4);
                }
            }

            if (h < 0) h += 360;

            // Skaliere hsv Werte für Emgu.Cv 
            return new Hsv((byte)(h / 2), (byte)(s * 255), (byte)(v * 255));
        }

    }

}