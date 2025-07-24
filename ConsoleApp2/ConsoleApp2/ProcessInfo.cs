using System.Diagnostics;

namespace ConsoleApp2
{
    internal class ProcessInfo
    {
        public static string ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory(), 
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(error))
                {
                    output += $"\nError: {error}";
                }
                return output;
            }
        }

        public static string ConstructHTL(List<Prs> PrList)
        {
            string htmlContent = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body> <Table>
<tr>
<th>PR Number</th>
        <th>Auther</th>
    <th>Reviewer</th>
    <th>Comments</th>
<th> PR Stauts </th>
</tr>";

            if (PrList.Count > 0)
            {
                for (int i = 0; i < PrList.Count; i++)
                {
                    var prRowItem = PrList[i]; 
                    htmlContent = htmlContent + "<tr> <td  >"+ prRowItem.PrNumber + "</td>";
                    htmlContent = htmlContent + " <td>" + prRowItem.prComments[0].Author + "</td>";
                    htmlContent = htmlContent + "<td> <table>";
                    for (int j = 0; j < prRowItem.stats.Counts.Count; j++)
                    {                      
                        htmlContent = htmlContent + " <tr> <td>" + prRowItem.stats.Counts.ElementAt(j).Key  + "</td> </tr>";
                    }
                    htmlContent = htmlContent + "</table> <td> <table>";
                    for (int j = 0; j < prRowItem.prComments.Count; j++)
                    {
                        htmlContent = htmlContent + " <tr> <td>" + prRowItem.prComments[j].CommentText + "</td> </tr>";
                    }
                    htmlContent = htmlContent + "</table> <td>" + prRowItem.PrState+ "</tr>";
                    htmlContent = htmlContent + "</tr>";
                }

                htmlContent = htmlContent + " </table>";

            }

            return htmlContent;
        }
    }
}
