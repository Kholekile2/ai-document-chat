using AiDocChat.Api.Services;
using Microsoft.IdentityModel.Tokens;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace AiDocChat.Api.Services
{
    public class PdfService
    {
        // Extracts all text from a PDF file given its path on disk.
        // PdfPig reads the PDF structure and pulls text from each page.
        public string ExtractText(string filePath)
        {
            using var document = PdfDocument.Open(filePath);
            var textBuilder = new System.Text.StringBuilder();

            foreach (Page page in document.GetPages())
            {
                var words = page.GetWords().ToList();
                if (!words.Any()) continue;

                // Sort top-to-bottom (PDF Y axis is inverted, so descending),
                // then left-to-right within each line.
                var sortedWords = words
                    .OrderByDescending(w => Math.Round(w.BoundingBox.Bottom, 1))
                    .ThenBy(w => w.BoundingBox.Left)
                    .ToList();

                var pageBuilder = new System.Text.StringBuilder();
                UglyToad.PdfPig.Content.Word? previousWord = null;

                foreach (var word in sortedWords)
                {
                    if (previousWord == null)
                    {
                        pageBuilder.Append(word.Text);
                    }
                    else
                    {
                        var verticalGap = Math.Abs(word.BoundingBox.Bottom - previousWord.BoundingBox.Bottom);

                        // Use the average character width of the previous word as
                        // the line-break threshold — more reliable than a fixed value
                        // since PDF coordinate units vary between documents.
                        var avgCharWidth = previousWord.BoundingBox.Width / Math.Max(previousWord.Text.Length, 1);

                        if (verticalGap > avgCharWidth)
                        {
                            // Significant vertical shift — treat as a new line
                            pageBuilder.AppendLine();
                            pageBuilder.Append(word.Text);
                        }
                        else
                        {
                            // Same line — always insert a space between words
                            pageBuilder.Append(' ');
                            pageBuilder.Append(word.Text);
                        }
                    }

                    previousWord = word;
                }

                textBuilder.AppendLine(pageBuilder.ToString());
            }

            return textBuilder.ToString().Trim();
        }

        // Splits extracted text into overlapping chunks suitable for embedding.
        // WHAT IS CHUNKING: LLMs have a limit on how much text they can process
        // at once. We split the document into smaller pieces (chunks) so we can
        // find and send only the most relevant pieces when answering a question.
        // OVERLAP: Each chunk shares some text with the next one to avoid cutting
        // a sentence or idea in half at a chunk boundary.
        public List<string> ChunkText(string text, int chunkSize = 500, int overlap = 50)
        {
            var chunks = new List<string>();
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return chunks;

            int start = 0;

            while (start < words.Length)
            {
                int end = Math.Min(start + chunkSize, words.Length);
                var chunk = string.Join(" ", words[start..end]);
                chunks.Add(chunk);
                start += chunkSize - overlap;
            }

            return chunks;
        }
    }
}
