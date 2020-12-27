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
        string _url = "https://www.jeuxvideo.com/tous-les-jeux/";
        string _titleLandmark = "class=\"gameTitleLink__196nPy\"";
        string _gradeLandmark = "class=\"editorialRating__1tYu_r\"";
        public List<GameInfo> gameInfos;

        public JVCScrapper()
        {
            gameInfos = new List<GameInfo>();
        }

        public void GetGrades()
        {
            const int threadsNb = 8;
            var tasks = new ScrapperJob[threadsNb];
            int pageNb = 1;

            for (int i = 0; i < threadsNb; ++i)
            {
                tasks[i] = new ScrapperJob(pageNb);
                ++pageNb;
            }
            foreach (ScrapperJob job in tasks)
                job.StartJob();
            while (true)
            {
                for (int i = 0; i < tasks.Length; ++i)
                {
                    if (tasks[i].task.IsCompleted)
                    {
                        if (pageNb > 2000)
                            return;
                        gameInfos.AddRange(tasks[i].GetGameInfos());
                        tasks[i].pageNb = pageNb;
                        tasks[i].StartJob();
                        ++pageNb;
                    }
                }
            }
        }

        public void DisplayGrades()
        {
            foreach (GameInfo info in gameInfos)
                Console.WriteLine(info.name + " - " + info.grade + "/20");
        }

        class ScrapperJob
        {
            public Task<bool> task;
            string _url = "https://www.jeuxvideo.com/tous-les-jeux/";
            string _titleLandmark = "class=\"gameTitleLink__196nPy\"";
            string _gradeLandmark = "class=\"editorialRating__1tYu_r\"";
            List<GameInfo> _gameInfos;
            public int pageNb;

            public ScrapperJob(int pageNb)
            {
                this.pageNb = pageNb;
            }

            public void StartJob()
            {
                _gameInfos = new List<GameInfo>();
                task = Task.Run(() => ParsePage(pageNb));
            }

            public List<GameInfo> GetGameInfos()
            {
                return _gameInfos;
            }

            async Task<bool> ParsePage(int pageNb)
            {
                var client = new HttpClient();
                var result = await client.GetStringAsync(_url + "?p=" + pageNb.ToString());

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
                        _gameInfos.Add(new GameInfo
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
                return true;
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
        }
    }
}
