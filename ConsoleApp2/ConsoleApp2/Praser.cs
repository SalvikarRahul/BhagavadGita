using static ConsoleApp2.GetComments;

namespace ConsoleApp2
{
    public class PullReuqest
    {
        public int? Number { get; set; }
        public string? State { get; set; }
    }

    public class Prs
    {
       
        public string PrNumber { get; set; }
        public List<Comment> prComments { get; set; }
        public string PrState {get;set;}
        public CommentStatistics stats  { get; set; }
        public string fileChanges  { get; set; }
    }
}
