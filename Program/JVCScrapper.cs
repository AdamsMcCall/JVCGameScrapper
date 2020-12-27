using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Program
{
    public class JVCScrapper
    {
        private readonly int _pagesLimit;
        private int _pageNumber = 1;
        private string _url = "https://www.jeuxvideo.com/tous-les-jeux/";
        private string _titleLandmark = "class=\"gameTitleLink__196nPy\"";
        private string _gradeLandmark = "class=\"editorialRating__1tYu_r\"";
        public List<GameInfo> gameInfos;
        private Object _pageNumberLock = new Object();
        private Object _gameInfoLock = new Object();

        public JVCScrapper(int pagesLimit)
        {
            gameInfos = new List<GameInfo>();
            _pagesLimit = pagesLimit;
        }

        async void ThreadLoop(object pageNb)
        {
            int threadNb = (int)pageNb;
            int currentPageNb = (int)pageNb;
            List<GameInfo> localGameInfos = new List<GameInfo>();

            while (currentPageNb < _pagesLimit)
            {
                //Console.WriteLine("> Thread no " + threadNb.ToString() + " is still alive !");
                localGameInfos.AddRange(await ParsePage(currentPageNb));
                currentPageNb = GetNextPageNumber();
            }
            lock (_gameInfoLock)
            {
                gameInfos.AddRange(localGameInfos);
            }
        }

        async Task<List<GameInfo>> ParsePage(int pageNb)
        {
            var client = new HttpClient();
            var result = await client.GetStringAsync(_url + "?p=" + pageNb.ToString());
            var pageGameInfos = new List<GameInfo>();

            try
            {
                while (true)
                {
                    var title = GetContent(result, _titleLandmark);
                    title = title.Replace("&#x27;", "'");
                    var grade = GetContent(result, _gradeLandmark);
                    result = result.Substring(result.IndexOf(_gradeLandmark) + 50);
                    result = result.Substring(result.IndexOf(_titleLandmark) - 50);
                    grade = FormatGrade(grade);
                    if (grade == "-")
                        continue;
                    pageGameInfos.Add(new GameInfo
                    {
                        name = title,
                        grade = grade
                    });
                    //Console.WriteLine(title + " - " + grade);
                }
            }
            catch
            {

            }
            Console.WriteLine("Page " + pageNb.ToString() + " done");
            return pageGameInfos;
        }

        string GetContent(string data, string landmark)
        {
            var idx = data.IndexOf(landmark);
            var output = data.Substring(idx);
            idx = output.IndexOf('>') + 1;
            output = output.Substring(idx);
            if (output[0] == '<')
                output = output.Substring(output.IndexOf('>') + 1);
            output = output.Remove(output.IndexOf('<'));
            return output;
        }

        string FormatGrade(string rawGrade)
        {
            var output = rawGrade.Remove(rawGrade.IndexOf('/'));

            if (float.TryParse(output, out _))
                return output;
            else
                return "-";
        }

        int GetNextPageNumber()
        {
            lock (_pageNumberLock)
            {
                int nextPageNumber = _pageNumber;
                _pageNumber += 1;

                return nextPageNumber;
            }
        }

        public void GetGrades()
        {
            var pool = new MyThreadPool();
            var threadNumber = 4;

            for (int i = 0; i < threadNumber; ++i)
            {
                pool.StartThread(new ParameterizedThreadStart(ThreadLoop), _pageNumber);
                ++_pageNumber;
            }
            //pool.WaitAllThreads();
            while (true)
            {

            }
        }

        public void DisplayGrades()
        {
            foreach (GameInfo info in gameInfos)
                Console.WriteLine(info.name + " - " + info.grade + "/20");
        }
    }
}
