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
            text-align: center;
            border-bottom: 2px solid #d1d9e0;
        }}
        
        td {{
            padding: 20px;
            border-bottom: 1px solid #e1e4e8;
            vertical-align: center;
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
       
<Table>
<tr>
<th>PR Number</th>
        <th>Author</th>
    <th>Reviewer</th>
  
<th>Comments</th>
  <th>File changes</th>
<th> PR Status</th>
</tr>";

            if (PrList.Count > 0)
            {
                int count = PrList.Count > 10 ? 10 : PrList.Count;
                for (int i = 0; i < count; i++)
                {
                    var prRowItem = PrList[i];
                    htmlContent = htmlContent + "<tr> <td  >" + prRowItem.PrNumber + "</td>";
                    if (prRowItem.prComments != null && prRowItem.prComments.Count > 0)
                    {
                        htmlContent = htmlContent + " <td>" + prRowItem.prComments[0].Author + "</td>";
                    }
                    else
                    {
                        htmlContent = htmlContent + "<td> </td>";
                    }
                   
                    if (prRowItem.stats.Counts != null && prRowItem.stats.Counts.Count > 0)
                    {
                        htmlContent = htmlContent + "<td> <table>";
                        for (int j = 0; j < prRowItem.stats.Counts.Count; j++)
                        {
                            htmlContent = htmlContent + " <tr> <td>" + prRowItem.stats.Counts.ElementAt(j).Key + "</td> </tr>";
                        }
                        htmlContent = htmlContent + "</table>";
                    }

                    if (prRowItem.prComments != null && prRowItem.prComments.Count > 0)
                    {
                        htmlContent = htmlContent + " <td> <table>";
                        for (int j = 0; j < prRowItem.prComments.Count; j++)
                        {
                            htmlContent = htmlContent + " <tr> <td>" + prRowItem.prComments[j].CommentText + "</td> </tr>";
                        }
                        htmlContent = htmlContent + "</table>";
                    }
                       
                    htmlContent = htmlContent + " <td>" + prRowItem.fileChanges + "</td>";
                    htmlContent = htmlContent + " <td>" + prRowItem.PrState + "</td>";
                    htmlContent = htmlContent + "</tr>";
                }

                htmlContent = htmlContent + " </table>";
            }

            return htmlContent;
        }
    }
}
