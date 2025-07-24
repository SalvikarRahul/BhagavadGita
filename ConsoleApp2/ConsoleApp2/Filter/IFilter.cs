namespace ConsoleApp2.Filter
{
    internal interface IFilter
    {
        List<PullReuqest> ListAllPR();

        List<PullReuqest> ListByAuthor(string author);

        List<PullReuqest> ListByState(string state);

        List<PullReuqest> ListByDuration(DateTime startdate, DateTime enddate);

        List<PullReuqest> DeSerializer(string output);
    }
}
