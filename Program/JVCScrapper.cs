using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
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
        private int _threadsDone = 0;
        private string _url = "https://www.jeuxvideo.com/tous-les-jeux/";
        private string _titleLandmark = "class=\"gameTitleLink__196nPy\"";
        private string _linkLandmark = "/jeux/";
        private string _gradeLandmark = "class=\"editorialRating__1tYu_r\"";
        public List<GameInfo> gameInfos;
        private Object _pageNumberLock = new Object();
        private Object _gameInfoLock = new Object();
        private Object _threadsDoneLock = new Object();

        public JVCScrapper(int pagesLimit)
        {
            gameInfos = new List<GameInfo>();
            _pagesLimit = pagesLimit;
        }

        void ThreadLoop(object pageNb)
        {
            var options = new FirefoxOptions();
            options.AddArgument("--headless");
            IWebDriver webDriver = new FirefoxDriver(options);
            int threadNb = (int)pageNb;
            int currentPageNb = (int)pageNb;
            List<GameInfo> localGameInfos = new List<GameInfo>();

            while (currentPageNb <= _pagesLimit)
            {
                localGameInfos.AddRange(ParsePage(currentPageNb, webDriver));
                currentPageNb = GetNextPageNumber();
            }
            lock (_gameInfoLock)
            {
                gameInfos.AddRange(localGameInfos);
            }
            webDriver.Close();
            lock (_threadsDoneLock)
            {
                _threadsDone += 1;
            }
            Console.WriteLine("Thread " + threadNb.ToString() + " done !");
        }

        List<GameInfo> ParsePage(int pageNb, IWebDriver webDriver)
        {
            webDriver.Url = _url + "?p=" + pageNb.ToString();
            var result = webDriver.PageSource;
            var pageGameInfos = new List<GameInfo>();

            try
            {
                while (true)
                {
                    var link = GetLink(result);
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
                        grade = grade,
                        link = "https://www.jeuxvideo.com" + link
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

        string GetLink(string data)
        {
            var idx = data.IndexOf(_titleLandmark);
            var output = data.Substring(BackIndexOf(data, idx, '<'));
            output = output.Remove(output.IndexOf('>'));
            output = output.Substring(output.IndexOf(_linkLandmark));
            output = output.Remove(output.IndexOf('"'));
            return output;
        }

        int BackIndexOf(string str, int idx, char c)
        {
            while (str[idx] != c && idx > -1)
                --idx;
            return idx + 1;
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
            while (_threadsDone < threadNumber)
            {
                //Wait for all threads to be done
            }
        }

        public void DisplayGrades()
        {
            foreach (GameInfo info in gameInfos)
                Console.WriteLine(info.name + " - " + info.grade + "/20 - " + info.link);
        }
    }
}
