using Newtonsoft.Json;
using System.Globalization;

namespace ConsoleApp2.Filter
{
    public class Filter : IFilter
    {
        public List<PullReuqest> ListAllPR()
        {
            string command = "gh pr list --state all --json number,state";
            return DeSerializer(ProcessInfo.ExecuteCommand(command));
        }

        public List<PullReuqest> ListByAuthor(string author)
        {
            string command = $"gh pr list --author {author} --state all --json number,state";
            return DeSerializer(ProcessInfo.ExecuteCommand(command));
        }

        public List<PullReuqest> ListByState(string state)
        {
            string command = $"gh pr list --state {state} --json number,state";
            return DeSerializer(ProcessInfo.ExecuteCommand(command));
        }

        public List<PullReuqest> ListByDuration(DateTime startdate , DateTime enddate)
        {
            string command = $"gh pr list --search \"created:{startdate:yyyy-MM-dd}..{enddate:yyyy-MM-dd}\" --state all --json number,state";
            return DeSerializer(ProcessInfo.ExecuteCommand(command));
        }

        public List<PullReuqest> DeSerializer(string output)
            => JsonConvert.DeserializeObject<List<PullReuqest>>(output) ?? new List<PullReuqest>();
       
    }
}