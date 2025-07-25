using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace ConsoleApp2
{
    public class GetComments
    {
        private const string OutputDirectory = "pr_comments";
        //private const string Repository = "SalvikarRahul/BhagavadGita";
        public static List<Prs> PrList;
        public static async Task Main(string[] args)
        {
            DateTime fromDate = DateTime.Now.AddDays(-30);
            DateTime toDate = DateTime.Now; 
            args = new string[] { "2025-07-01","2025-07-25" };
            if (args.Length != 2)
            {
                Console.WriteLine("Provide proper input");
                return;
            }


            if (DateTime.TryParse(args[0], out DateTime dt1))
            {
                fromDate = dt1;
                if (DateTime.TryParse(args[1], out DateTime dt2))
                {
                    toDate = dt2;
                }
                else
                {
                    Console.WriteLine("Provide proper input");
                    return;
                }
            }
            else
            {
                Console.WriteLine("Provide proper input");
                return;
            }

            PrList = new List<Prs>();
            var program = new GetComments();
            await program.RunAsync(fromDate, toDate);
            Console.Read();
        }

        public async Task RunAsync(DateTime fromDate, DateTime toDate)
        {
            // List all PR numbers you want to process here

            var processInfo = new ProcessStartInfo()
            {
                FileName = "gh",
                Arguments = " pr list --state all --json number,state",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,

            };

            using var process = Process.Start(processInfo);
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Error running command: pr list --state all --json number,state --search \"created:{fromDate:yyyy-MM-dd}..{toDate:yyyy-MM-dd}\"");
                Console.WriteLine($"Error: {error}");
                Environment.Exit(1);
            }
            File.WriteAllText("pr list --state all --json number,state", output.Trim());

            // user parser

            var temp = JsonConvert.DeserializeObject<List<PullReuqest>>(output.Trim());
            List<string> prNumbers = new List<string>();
            int tempCount = temp.Count > 10 ? 10 : temp.Count;
            for (int i = 0; i < tempCount; i++)
            {
                prNumbers.Add(temp[i].Number.ToString());
            }
            //var prNumbers = new List<string> { "1","2" };

            if (!prNumbers.Any())
            {
                Console.WriteLine("No PR numbers specified. Please add PR numbers to the prNumbers list.");
                Environment.Exit(1);
            }

            Console.WriteLine($"Processing {prNumbers.Count} PRs...");
            Console.WriteLine(new string('=', 50));

            int successCount = 0;
            var failedPrs = new List<string>();

            foreach (var prNumber in prNumbers)
            {
                try
                {
                    Console.WriteLine($"\n🔄 Processing PR #{prNumber}...");

                    // Get PR participants (author and reviewers)
                    var participants = await GetPrParticipantsAsync(prNumber);
                    PrintParticipantsSummary(prNumber, participants);

                    // Extract review comments (excluding author responses)
                    var comments = await ExtractReviewCommentsAsync(prNumber, participants);

                    // Print tabular summary of reviewer comments
                    PrintReviewerCommentsSummary(prNumber, comments, participants);

                    // Save in all formats: text, HTML, CSV, and participants info
                    await SaveCommentsToFileAsync(prNumber, comments);

                    var processInfoFile = new ProcessStartInfo()
                    {
                        FileName = "gh",
                        Arguments = $" pr diff --name-only {prNumber}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,

                    };

                    using var processfile = Process.Start(processInfoFile);
                    var outputfile = processfile.StandardOutput.ReadToEnd();
                    processfile.WaitForExit();
                    var errorfile = processfile.StandardError.ReadToEnd();

                    processfile.WaitForExit();

                    if (processfile.ExitCode != 0)
                    {
                        Console.WriteLine($"Error running command: pr list --state all --json number,state");
                        Console.WriteLine($"Error: {errorfile}");
                        Environment.Exit(1);
                    }

                    PrList.Add(new Prs()
                    {
                        PrNumber = prNumber,
                        prComments = comments,
                        stats = GetCommentStatistics(comments),
                        PrState = participants.prState,
                        fileChanges= outputfile.Trim()

                    }) ;
                    //await SaveCommentsToHtmlAsync(prNumber, comments);
                    //await SaveCommentsToCsvAsync(prNumber, comments);
                    //await SaveParticipantsInfoAsync(prNumber, participants, comments);
                    //await SaveParticipantsCsvAsync(prNumber, participants, comments);


                   
                    //  File.WriteAllText("pr list --state all --json number,state", outputfile.Trim());
                    
                      successCount++;
                    Console.WriteLine($"✅ PR #{prNumber} completed successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error processing PR #{prNumber}: {ex.Message}");
                    failedPrs.Add(prNumber);
                }
            }
         
            string htmlData = ProcessInfo.ConstructHTL(PrList);
            SaveCommentsToHtml(htmlData);
            //PrintFinalSummary(successCount, prNumbers.Count, failedPrs);
        }



        #region Utility Methods

        private async Task<string> RunCommandAsync(string command, string prNumber)
        {
            try
            {
                var processInfo = new ProcessStartInfo()
                {
                    FileName = "gh",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,

                };

                using var process = Process.Start(processInfo);
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Error running command: {command}");
                    Console.WriteLine($"Error: {error}");
                    Environment.Exit(1);
                }
                //File.WriteAllText("pr_info" + prNumber + ".json", output.Trim());

                return output.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running command: {command}");
                Console.WriteLine($"Exception: {ex.Message}");
                Environment.Exit(1);
                return string.Empty;
            }
        }

        private async Task<JsonDocument> FetchPrJsonAsync(string prNumber, string jsonFields, string fileSuffix = "", bool IsreadOnly = false)
        {
            var tempFile = string.Concat("pr_", fileSuffix, prNumber, ".json"); //"/tmp/pr_{fileSuffix}_{prNumber}.json";

            //if (!File.Exists(tempFile) & !IsreadOnly)
            //{
            //    File.Create(tempFile);

            //}
            //if (!IsreadOnly)
            //{
            // await RunCommandAsync($" pr view {prNumber} --json {jsonFields} > {tempFile}");
            var prdata = await RunCommandAsync($" pr view {prNumber} --json {jsonFields}", prNumber);
            //}

            // await RunCommandAsync($" git fetch origin + refs / pull/*/head:refs/remotes/origin/pr/*

            try
            {
                var jsonContent = prdata; //await File.ReadAllTextAsync(tempFile);
                var jsonDocument = JsonDocument.Parse(jsonContent);

                // Clean up temp file
                try
                {
                    if (IsreadOnly)
                    {
                        //File.Delete(tempFile);
                    }
                }
                catch (FileNotFoundException) { }

                return jsonDocument;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading PR data: {ex.Message}");
                Environment.Exit(1);
                return null;
            }
        }

        private bool IsMeaningfulLine(string line, int minLength = 3)
        {
            line = line.Trim();
            return !string.IsNullOrEmpty(line) &&
                   !line.StartsWith("Reviewed") &&
                   !line.StartsWith("Previously") &&
                   !line.StartsWith("status:") &&
                   !line.Contains("Reviewable") &&
                   !line.StartsWith("___") &&
                   !line.StartsWith("*") &&
                   !new[] { "changes done", "Done", "Fixed", "Addressed" }.Contains(line) &&
                   line.Length > minLength;
        }

        private string FormatCommentCount(int count)
        {
            return $"{count} comment{(count != 1 ? "s" : "")}";
        }

        private Dictionary<string, int> GetReviewerCommentCounts(List<Comment> comments)
        {
            var counts = new Dictionary<string, int>();
            foreach (var comment in comments)
            {
                var reviewer = GetReviewer(comment);
                counts[reviewer] = counts.ContainsKey(reviewer) ? counts[reviewer] + 1 : 1;
            }
            return counts;
        }

        private CommentStatistics GetCommentStatistics(List<Comment> comments)
        {
            var counts = GetReviewerCommentCounts(comments);
            var total = comments.Count;
            var active = counts.Count;
            var avg = active > 0 ? (double)total / active : 0;

            return new CommentStatistics
            {
                Counts = counts,
                Total = total,
                Active = active,
                Average = avg
            };
        }

        private string CleanCommentText(string text)
        {
            // Remove code blocks
            text = Regex.Replace(text, @"```.*?```", "", RegexOptions.Singleline);
            // Normalize whitespace
            text = Regex.Replace(text, @"\n+", " ");
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }

        private string GetOutputDirectory()
        {
            Directory.CreateDirectory(OutputDirectory);
            return OutputDirectory;
        }

        private string GetReviewer(Comment comment)
        {
            return comment.Author ?? "Unknown";
        }

        private Dictionary<string, List<string>> GroupCommentsByReviewer(List<Comment> comments)
        {
            var grouped = new Dictionary<string, List<string>>();
            foreach (var comment in comments)
            {
                var reviewer = GetReviewer(comment);
                if (!grouped.ContainsKey(reviewer))
                    grouped[reviewer] = new List<string>();
                grouped[reviewer].Add(comment.CommentText ?? "");
            }
            return grouped;
        }

        #endregion

        #region Data Models

        public class Participants
        {
            public string Author { get; set; } = "";
            public JsonDocument jsonDocument { get; set; }
            public List<string> Reviewers { get; set; } = new();
            public string prState { get; set; }

        }

        public class Comment
        {
            public string Author { get; set; } = "";
            public string Date { get; set; } = "";
            public string Code { get; set; } = "";
            public string CommentText { get; set; } = "";



        }

        public class CommentStatistics
        {
            public Dictionary<string, int> Counts { get; set; } = new();
            public int Total { get; set; }
            public int Active { get; set; }
            public double Average { get; set; }
        }

        public class CodeCommentPair
        {
            public string Code { get; set; } = "";
            public string CommentText { get; set; } = "";
        }

        #endregion

        #region Data Extraction Methods

        private async Task<Participants> GetPrParticipantsAsync(string prNumber)
        {
            Console.WriteLine($"Fetching PR participants for PR {prNumber}...");

            var jsonDoc = await FetchPrJsonAsync(prNumber, "author,reviews,comments,state", "info");
            var root = jsonDoc.RootElement;

            // Extract author
            var author = "Unknown";
            if (root.TryGetProperty("author", out var authorElement) &&
                authorElement.TryGetProperty("login", out var loginElement))
            {
                author = loginElement.GetString() ?? "Unknown";
            }

            // Extract all unique reviewers
            var reviewers = new HashSet<string>();
            if (root.TryGetProperty("reviews", out var reviewsElement))
            {
                foreach (var review in reviewsElement.EnumerateArray())
                {
                    if (review.TryGetProperty("author", out var reviewAuthor) &&
                        reviewAuthor.TryGetProperty("login", out var reviewLogin))
                    {
                        var reviewerLogin = reviewLogin.GetString();
                        if (!string.IsNullOrEmpty(reviewerLogin) && reviewerLogin != author)
                        {
                            reviewers.Add(reviewerLogin);
                        }
                    }
                }
            }

            root.TryGetProperty("state", out var prStatus);
            JsonDocument jsonDocument = jsonDoc;

            return new Participants
            {

                Author = author,
                Reviewers = reviewers.OrderBy(r => r).ToList(),
                jsonDocument = jsonDocument,
                prState = prStatus.GetString()
            };
        }

        private async Task<List<Comment>> ExtractReviewCommentsAsync(string prNumber, Participants participants)
        {

            string prAuthor = participants.Author;


            Console.WriteLine($"Fetching reviews for PR {prNumber}...");

            using var jsonDoc = participants.jsonDocument;//await FetchPrJsonAsync(prNumber, "", "info", true);
            Console.Write(jsonDoc);
            var root = jsonDoc.RootElement;
            var comments = new List<Comment>();

            if (!root.TryGetProperty("comments", out var reviewsElement))
                return comments;

            foreach (var review in reviewsElement.EnumerateArray())
            {
                var body = "";
                var author = "Unknown";
                var submittedAt = "";

                if (review.TryGetProperty("body", out var bodyElement))
                    body = bodyElement.GetString() ?? "";

                if (review.TryGetProperty("author", out var authorElement) &&
                    authorElement.TryGetProperty("login", out var loginElement))
                    author = loginElement.GetString() ?? "Unknown";

                if (review.TryGetProperty("submittedAt", out var dateElement))
                    submittedAt = dateElement.GetString() ?? "";

                // Skip comments from the PR author - we only want reviewer comments
                //if (author == prAuthor)
                //    continue;

                // Clean up the body text but preserve code blocks and comments
                body = CleanReviewBody(body);

                comments.Add(new Comment
                {
                    Author = author,
                    Date = submittedAt,
                    CommentText = body
                });

                // Find code blocks and associated comments
                var codeCommentPairs = ExtractCodeCommentPairs(body);

                if (codeCommentPairs.Any())
                {
                    foreach (var pair in codeCommentPairs)
                    {
                        comments.Add(new Comment
                        {
                            Author = author,
                            Date = submittedAt,
                            Code = pair.Code,
                            CommentText = pair.CommentText
                        });
                    }
                }
                else
                {
                    // Fallback: look for any meaningful comments without code blocks
                    var bodyNoCode = Regex.Replace(body, @"```.*?```", "", RegexOptions.Singleline);
                    var meaningfulComment = ExtractMeaningfulComment(bodyNoCode, 10);

                    if (!string.IsNullOrEmpty(meaningfulComment))
                    {
                        comments.Add(new Comment
                        {
                            Author = author,
                            Date = submittedAt,
                            CommentText = meaningfulComment
                        });
                    }
                }
            }

            return comments;
        }

        private string CleanReviewBody(string body)
        {

            // Remove HTML tags and images
            body = Regex.Replace(body, @"<[^>]*>", "");
            // Remove Reviewable links and metadata
            body = Regex.Replace(body, @"\*\[.*?\]\(.*?\)\*.*", "");
            body = Regex.Replace(body, @"> \*\[Reviewable\].*", "");
            body = Regex.Replace(body, @"<!-- Sent from Reviewable\.io -->", "");
            // Remove blockquotes and details sections
            body = Regex.Replace(body, @"<details>.*?</details>", "", RegexOptions.Singleline);
            body = Regex.Replace(body, @"<blockquote>.*?</blockquote>", "", RegexOptions.Singleline);
            return body;
        }

        private List<CodeCommentPair> ExtractCodeCommentPairs(string body)
        {
            var pairs = new List<CodeCommentPair>();
            var parts = Regex.Split(body, @"(```.*?```)", RegexOptions.Singleline);

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("```") && i + 1 < parts.Length)
                {
                    var codeBlock = parts[i];
                    var nextText = i + 1 < parts.Length ? parts[i + 1] : "";

                    var meaningfulLines = nextText.Split('\n')
                        .Where(line => IsMeaningfulLine(line, 3))
                        .ToList();

                    if (meaningfulLines.Any())
                    {
                        var commentText = string.Join("\n", meaningfulLines);
                        var keywords = new[] { "why", "what", "how", "should", "can", "could", "?", "same", "this" };

                        if (keywords.Any(word => commentText.ToLower().Contains(word)))
                        {
                            pairs.Add(new CodeCommentPair
                            {
                                Code = codeBlock,
                                CommentText = commentText
                            });
                        }
                    }
                }
            }

            return pairs;
        }

        private string ExtractMeaningfulComment(string body, int minLength)
        {
            var meaningfulLines = body.Split('\n')
                .Where(line => IsMeaningfulLine(line, minLength))
                .ToList();

            if (meaningfulLines.Any())
            {
                var commentText = string.Join("\n", meaningfulLines);
                var keywords = new[] { "why", "what", "how", "should", "can", "could", "?" };

                if (keywords.Any(word => commentText.ToLower().Contains(word)))
                {
                    return commentText;
                }
            }

            return "";
        }

        #endregion

        #region Output Methods

        private async Task SaveCommentsToFileAsync(string prNumber, List<Comment> comments)
        {
            var outputDir = GetOutputDirectory();
            var outputFile = Path.Combine(outputDir, $"pr_{prNumber}_reviewer_comments.txt");

            var stats = GetCommentStatistics(comments);
            var reviewerCounts = stats.Counts;

            var content = new StringBuilder();
            content.AppendLine($"PR #{prNumber} - Reviewer Comments (Author responses excluded)");
            content.AppendLine(new string('=', 65));
            content.AppendLine("📊 COMMENT STATISTICS:");
            content.AppendLine($"   Total Reviewer Comments: {stats.Total}");
            content.AppendLine($"   Active Reviewers: {stats.Active}");

            if (reviewerCounts.Any())
            {
                content.AppendLine("   Comments per Reviewer:");
                foreach (var kvp in reviewerCounts.OrderBy(k => k.Key))
                {
                    content.AppendLine($"     • {kvp.Key}: {FormatCommentCount(kvp.Value)}");
                }
            }

            content.AppendLine(new string('=', 65));
            content.AppendLine();

            if (!comments.Any())
            {
                content.AppendLine("No reviewer questions/comments found.");
            }
            else
            {
                for (int i = 0; i < comments.Count; i++)
                {
                    var comment = comments[i];
                    content.AppendLine($"Comment #{i + 1} - Reviewer: {comment.Author}");

                    if (!string.IsNullOrEmpty(comment.Code))
                    {
                        content.AppendLine($"Code being reviewed:\n{comment.Code}\n");
                    }

                    var cleaned = CleanCommentText(comment.CommentText ?? "");
                    content.AppendLine($"Reviewer Feedback: {cleaned}");
                    content.AppendLine(new string('=', 65));
                    content.AppendLine();
                }
            }

            await File.WriteAllTextAsync(outputFile, content.ToString());
            Console.WriteLine($"Reviewer comments saved to: {outputFile}");
            Console.WriteLine($"📊 Total reviewer comments: {stats.Total}");
        }

        private async Task SaveCommentsToHtmlAsync(string prNumber, List<Comment> comments)
        {
            var outputDir = GetOutputDirectory();
            var outputFile = Path.Combine(outputDir, $"pr_{prNumber}_reviewer_comments.html");

            var stats = GetCommentStatistics(comments);

            var htmlContent = GenerateHtmlContent(prNumber, comments, stats);

            await File.WriteAllTextAsync(outputFile, htmlContent);
            Console.WriteLine($"HTML reviewer comments saved to: {outputFile}");
            Console.WriteLine($"📊 HTML includes: {stats.Total} comments from {stats.Active} reviewers");
        }

        private void SaveCommentsToHtml(string htmlContent)
        {
            var outputDir = Path.Join(Directory.GetCurrentDirectory(), "pr_comments");// GetOutputDirectory();
            var outputFile = outputDir + $"/{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.html";
            //  File.Create(outputFile);


            File.WriteAllText(outputFile, htmlContent);
            Console.WriteLine($"HTML reviewer comments saved to: {outputFile}");

            OpenFile(outputFile);
        }

        private void OpenFile(string outputFilePath)
        {
            string folderPath = Path.Join(Directory.GetCurrentDirectory(), "pr_comments");

            try
            {
                if (File.Exists(outputFilePath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = outputFilePath,
                        UseShellExecute = true,
                        WorkingDirectory = folderPath,
                    };
                    Process.Start(psi);
                }
                else
                {
                    Console.WriteLine("File not found..");
                }
                Cleanup(folderPath);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void Cleanup(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    string[] txtFiles = Directory.GetFiles(folderPath, "*.txt");

                    foreach (string file in txtFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            //Console.WriteLine($"Deleted: {file}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async Task SaveCommentsToCsvAsync(string prNumber, List<Comment> comments)
        {
            var outputDir = GetOutputDirectory();
            var outputFile = Path.Combine(outputDir, $"pr_{prNumber}_reviewer_comments.csv");

            var stats = GetCommentStatistics(comments);
            var reviewerCounts = stats.Counts;
            var validComments = new List<Comment>();

            var content = new StringBuilder();
            content.AppendLine("Reviewer,Feedback");

            if (!comments.Any())
            {
                content.AppendLine("N/A,No reviewer questions/comments found.");
            }
            else
            {
                foreach (var comment in comments)
                {
                    var reviewer = GetReviewer(comment);
                    var commentText = CleanCommentText(comment.CommentText ?? "");

                    if (!string.IsNullOrEmpty(commentText) && commentText.Length > 3)
                    {
                        validComments.Add(comment);
                        var csvSafeText = commentText.Replace("\"", "\"\"");
                        content.AppendLine($"{reviewer},\"{csvSafeText}\"");
                    }
                }

                // Add summary section
                if (validComments.Any())
                {
                    content.AppendLine(",");
                    content.AppendLine("=== SUMMARY ===,=== STATISTICS ===");
                    content.AppendLine($"Total Comments,{stats.Total}");
                    content.AppendLine($"Active Reviewers,{stats.Active}");
                    content.AppendLine(",");
                    content.AppendLine("=== COUNTS PER REVIEWER ===,");

                    foreach (var kvp in reviewerCounts.OrderBy(k => k.Key))
                    {
                        content.AppendLine($"{kvp.Key},{FormatCommentCount(kvp.Value)}");
                    }
                }
            }

            await File.WriteAllTextAsync(outputFile, content.ToString());
            Console.WriteLine($"CSV reviewer comments saved to: {outputFile}");
            Console.WriteLine($"📊 Total CSV rows: {(validComments.Any() ? stats.Total : 1)} comments + summary");
        }

        private async Task SaveParticipantsInfoAsync(string prNumber, Participants participants, List<Comment> comments)
        {
            var outputDir = GetOutputDirectory();
            var outputFile = Path.Combine(outputDir, $"pr_{prNumber}_participants.txt");

            var stats = GetCommentStatistics(comments);
            var reviewerCommentCounts = stats.Counts;

            var content = new StringBuilder();
            content.AppendLine($"PR #{prNumber} - Participants Information");
            content.AppendLine(new string('=', 50));
            content.AppendLine();
            content.AppendLine($"📝 PR Author: {participants.Author}");
            content.AppendLine($"📊 Total Review Comments: {stats.Total}");
            content.AppendLine($"👥 Active Reviewers: {stats.Active}");
            content.AppendLine();
            content.AppendLine("Reviewers with Comment Counts:");

            if (participants.Reviewers.Any())
            {
                for (int i = 0; i < participants.Reviewers.Count; i++)
                {
                    var reviewer = participants.Reviewers[i];
                    var commentCount = reviewerCommentCounts.ContainsKey(reviewer) ? reviewerCommentCounts[reviewer] : 0;
                    content.AppendLine($"  {i + 1}. {reviewer} ({FormatCommentCount(commentCount)})");
                }
            }
            else
            {
                content.AppendLine("  No reviewers found.");
            }

            content.AppendLine();
            content.AppendLine("📈 Summary:");
            content.AppendLine($"   • Total Reviewers: {participants.Reviewers.Count}");
            content.AppendLine($"   • Reviewers with Comments: {stats.Active}");
            content.AppendLine($"   • Total Review Comments: {stats.Total}");

            if (stats.Total > 0 && stats.Active > 0)
            {
                content.AppendLine($"   • Average Comments per Active Reviewer: {stats.Average:F1}");
            }

            await File.WriteAllTextAsync(outputFile, content.ToString());
            Console.WriteLine($"Participants info with comment counts saved to: {outputFile}");
        }

        private async Task SaveParticipantsCsvAsync(string prNumber, Participants participants, List<Comment> comments)
        {
            var outputDir = GetOutputDirectory();
            var outputFile = Path.Combine(outputDir, $"pr_{prNumber}_participants.csv");

            var stats = GetCommentStatistics(comments);
            var reviewerCommentCounts = stats.Counts;

            var content = new StringBuilder();
            content.AppendLine("Role,Username,Comment_Count");

            // Write author
            content.AppendLine($"Author,{participants.Author},0");

            // Write reviewers
            if (participants.Reviewers.Any())
            {
                foreach (var reviewer in participants.Reviewers)
                {
                    var commentCount = reviewerCommentCounts.ContainsKey(reviewer) ? reviewerCommentCounts[reviewer] : 0;
                    content.AppendLine($"Reviewer,{reviewer},{commentCount}");
                }
            }
            else
            {
                content.AppendLine("Reviewer,No reviewers found,0");
            }

            // Add summary rows
            content.AppendLine(",,");
            content.AppendLine("=== SUMMARY ===,=== STATISTICS ===,=== COUNTS ===");
            content.AppendLine($"Total Reviewers,{participants.Reviewers.Count},");
            content.AppendLine($"Active Reviewers,{stats.Active},");
            content.AppendLine($"Total Comments,{stats.Total},");

            if (stats.Total > 0 && stats.Active > 0)
            {
                content.AppendLine($"Avg Comments/Reviewer,{stats.Average:F1},");
            }

            await File.WriteAllTextAsync(outputFile, content.ToString());
            Console.WriteLine($"Participants CSV with comment counts saved to: {outputFile}");
        }

        #endregion

        #region Display Methods

        private void PrintParticipantsSummary(string prNumber, Participants participants)
        {
            Console.WriteLine($"\n👥 PR #{prNumber} Participants:");
            Console.WriteLine(new string('-', 30));
            Console.WriteLine($"📝 Author: {participants.Author}");
            var reviewersList = participants.Reviewers.Any() ? string.Join(", ", participants.Reviewers) : "None";
            Console.WriteLine($"👁️  Reviewers ({participants.Reviewers.Count}): {reviewersList}");
        }

        private void PrintReviewerCommentsSummary(string prNumber, List<Comment> comments, Participants participants)
        {
            Console.WriteLine($"\n📝 PR #{prNumber} Reviewer Comments Summary:");
            Console.WriteLine(new string('=', 60));

            if (!comments.Any())
            {
                Console.WriteLine("❌ No reviewer comments found.");
                return;
            }

            var stats = GetCommentStatistics(comments);
            var reviewerComments = GroupCommentsByReviewer(comments);

            // Print table header
            Console.WriteLine($"{"Reviewer",-20} | {"Comments Count",-15} | Sample Comment");
            Console.WriteLine(new string('-', 60));

            // Print each reviewer's summary
            foreach (var kvp in reviewerComments.OrderBy(k => k.Key))
            {
                var reviewer = kvp.Key;
                var commentsList = kvp.Value;
                var commentCount = commentsList.Count;

                var sampleComment = commentsList.FirstOrDefault() ?? "N/A";
                if (sampleComment.Length > 50)
                {
                    sampleComment = sampleComment.Substring(0, 50) + "...";
                }
                sampleComment = sampleComment.Replace('\n', ' ').Replace('\r', ' ');

                Console.WriteLine($"{reviewer,-20} | {commentCount,-15} | {sampleComment}");
            }

            Console.WriteLine($"\n📊 Total reviewer comments: {stats.Total}");
            Console.WriteLine($"🗣️  Active reviewers: {stats.Active}");
            Console.WriteLine($"🚫 Excluded author responses from: {participants.Author}");
        }

        private void PrintFinalSummary(int successCount, int totalPrs, List<string> failedPrs)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("📊 FINAL SUMMARY:");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"   ✅ Successfully processed: {successCount}/{totalPrs} PRs");

            if (failedPrs.Any())
            {
                Console.WriteLine($"   ❌ Failed PRs: {string.Join(", ", failedPrs)}");
            }

            if (successCount > 0)
            {
                Console.WriteLine($"   📁 Output directory: {OutputDirectory}/");
                Console.WriteLine("\n   📄 Generated Files Per PR:");
                Console.WriteLine("      • _reviewer_comments.txt (with comment counts & statistics)");
                Console.WriteLine("      • _reviewer_comments.html (with enhanced counts display)");
                Console.WriteLine("      • _reviewer_comments.csv (with summary section)");
                Console.WriteLine("      • _participants.txt (with comment counts per reviewer)");
                Console.WriteLine("      • _participants.csv (with comment count column)");
                Console.WriteLine("\n   🔢 Features Added:");
                Console.WriteLine("      • 📊 Total comment counts in all outputs");
                Console.WriteLine("      • 👥 Active reviewer counts");
                Console.WriteLine("      • 📈 Comments per reviewer breakdown");
                Console.WriteLine("      • 📋 Summary statistics sections");
                Console.WriteLine("      • 🚫 Author responses excluded - Only reviewer feedback");
            }

            Console.WriteLine("\n🎉 All done! Check the files above for detailed comment counts and statistics.");
        }

        #endregion

        #region HTML Generation

        private string GenerateHtmlContent(string prNumber, List<Comment> comments, CommentStatistics stats)
        {
            var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>PR #{prNumber} - Review Comments</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 20px;
            background-color: #f6f8fa;
            color: #24292f;
        }}
        
        .container {{
            max-width: 1400px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        
        .header {{
            background: linear-gradient(135deg, #0366d6, #0969da);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        
        .header h1 {{
            margin: 0;
            font-size: 2.5em;
            font-weight: 600;
        }}
        
        .stats {{
            background: #f6f8fa;
            padding: 15px 30px;
            border-bottom: 1px solid #d1d9e0;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        
        .table-container {{
            padding: 0;
            overflow-x: auto;
        }}
        
        table {{
            width: 100%;
            border-collapse: collapse;
            background: white;
        }}
        
        th {{
            background: #f6f8fa;
            color: #24292f;
            font-weight: 600;
            padding: 15px 20px;
            text-align: left;
            border-bottom: 2px solid #d1d9e0;
        }}
        
        td {{
            padding: 20px;
            border-bottom: 1px solid #e1e4e8;
            vertical-align: top;
        }}
        
        tr:hover {{
            background-color: #f6f8fa;
        }}
        
        .author {{
            font-weight: 600;
            color: #0366d6;
        }}
        
        .code-block {{
            background: #f6f8fa;
            border: 1px solid #d1d9e0;
            border-radius: 6px;
            margin: 10px 0;
            overflow-x: auto;
            font-family: monospace;
            padding: 10px;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>PR #{prNumber}</h1>
            <p>Reviewer Comments & Code Analysis</p>
            <small style=""opacity: 0.8;"">🚫 Author responses excluded - Reviewer feedback only</small>
        </div>
        
        <div class=""stats"">
            <div><strong>📊 Total Reviewer Comments:</strong> {stats.Total}</div>
            <div><strong>👥 Active Reviewers:</strong> {stats.Active}</div>
            <div><strong>📅 Generated:</strong> {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}</div>
        </div>
        
        <div class=""table-container"">
            {GenerateHtmlTable(comments)}
        </div>
    </div>
</body>
</html>";

            return html;
        }

        private string GenerateHtmlTable(List<Comment> comments)
        {
            if (!comments.Any())
            {
                return @"
                <div style=""text-align: center; padding: 60px 30px;"">
                    <div style=""font-size: 4em; margin-bottom: 20px; opacity: 0.5;"">📝</div>
                    <h3>No reviewer questions/comments found</h3>
                    <p>This PR doesn't contain any extractable reviewer comments or questions.</p>
                </div>";
            }

            var tableBuilder = new StringBuilder();
            tableBuilder.AppendLine(@"
            <table>
                <thead>
                    <tr>
                        <th>Reviewer</th>
                        <th>Feedback & Comments</th>
                    </tr>
                </thead>
                <tbody>");

            foreach (var comment in comments)
            {
                var codeHtml = "";
                if (!string.IsNullOrEmpty(comment.Code))
                {
                    var codeText = System.Net.WebUtility.HtmlEncode(comment.Code);
                    codeHtml = $@"<div class=""code-block"">{codeText}</div>";
                }

                var commentText = CleanCommentText(comment.CommentText ?? "");
                commentText = System.Net.WebUtility.HtmlEncode(commentText);
                var commentHtml = $"<p>{commentText}</p>";

                var fullComment = codeHtml + commentHtml;

                tableBuilder.AppendLine($@"
                    <tr>
                        <td><div class=""author"">{comment.Author}</div></td>
                        <td>{fullComment}</td>
                    </tr>");
            }

            tableBuilder.AppendLine(@"
                </tbody>
            </table>");

            return tableBuilder.ToString();
        }
        #endregion

    }
}