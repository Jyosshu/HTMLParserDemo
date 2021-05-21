using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClassLibrary;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HTMLParserDemo
{
    public class ParseHTML : IParseHTML
    {
        //private readonly IConfiguration _config;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IDataAccess _dataAccess;
        private readonly ILogger<ParseHTML> _log;

        private List<Series> _series;
        private List<SeriesItem> _seriesItems;
        private List<Distributor> _distributors;

        public ParseHTML(IOptions<AppSettings> appSettings, IDataAccess dataAccess, ILogger<ParseHTML> log)
        {
            _appSettings = appSettings;
            _dataAccess = dataAccess;
            _log = log;
            _series = new List<Series>();
            _seriesItems = new List<SeriesItem>();
        }

        public void Run()
        {
            try
            {
                foreach (WebsiteToParse siteToParse in _appSettings.Value.WebsitesToParse)
                {
                    if (!Directory.Exists(siteToParse.Path))
                        throw new DirectoryNotFoundException($"The directory = { siteToParse.Path } does not exist.");

                    _distributors = _dataAccess.GetDistributorsFromDb();

                    string[] filesToParse = Directory.GetFiles(siteToParse.Path, "*.html", SearchOption.AllDirectories);

                    _log.LogInformation($"The directory = { siteToParse.Path } contained { filesToParse.Length } files.");

                    if (filesToParse.Length < 1)
                    {
                        _log.LogInformation("There were no files to parse.");
                    }
                    else
                    {
                        string previousSeriesTitle = "NotStarted";

                        foreach (string file in filesToParse)
                        {
                            if (siteToParse.FilenameWordsToSkip.Any(x => file.Contains(x, StringComparison.OrdinalIgnoreCase)))
                                continue;

                            // TODO: For each new Title (folder) add new Series to List<Series>

                            // TODO: parse files
                            HtmlDocument htmlDoc = new HtmlDocument();
                            htmlDoc.Load(file);

                            if (htmlDoc.DocumentNode != null)
                            {
                                HtmlNode baseContainerNode = htmlDoc.DocumentNode.SelectSingleNode("//" + siteToParse.BaseContainerTag);
                                if (baseContainerNode == null)
                                {
                                    _log.LogWarning($"The file = { file } is missing the expected <table> tag.  Skipping file.");
                                    continue;
                                }

                                HtmlNode titleNode = baseContainerNode.SelectSingleNode("//" + siteToParse.InnerTitleTag);
                                if (titleNode == null)
                                {
                                    _log.LogWarning($"The file = { file } is missing the expected <h2> tag.  Skipping file.");
                                    continue;
                                }

                                HtmlNode imageNode = baseContainerNode.SelectSingleNode("//" + siteToParse.InnerImageTag);
                                if (imageNode == null)
                                {
                                    _log.LogWarning($"The file = { file } is missing the expected <img> tag.  Skipping file.");
                                    continue;
                                }

                                string title = titleNode.InnerText;

                                string imagePath = imageNode.GetAttributeValue("src", string.Empty);
                                string imageCaption = imageNode.GetAttributeValue("alt", title);

                                string pNodeOne = string.Empty;
                                string pNodeTwo = string.Empty;

                                var pNodes = baseContainerNode.SelectNodes("//" + siteToParse.InnerTextTag);
                                if (pNodes != null && pNodes.Count > 1)
                                {
                                    if (pNodes.Count == 2)
                                    {
                                        pNodeOne = pNodes[0].InnerText.Trim();
                                        pNodeTwo = pNodes[1].InnerText.Trim();
                                    }
                                    else if (pNodes.Count > 2)
                                    {
                                        if (pNodes[0] != null && pNodes[1].InnerText.Contains("Details", StringComparison.OrdinalIgnoreCase))
                                        {
                                            pNodeOne = pNodes[1].InnerText.Trim();
                                            pNodeTwo = pNodes[2].InnerText.Trim();
                                        }
                                        else
                                        {
                                            pNodeOne = pNodes[0].InnerText.Trim();
                                            StringBuilder sb = new StringBuilder();

                                            for (int i = 1; i < pNodes.Count; i++)
                                            {
                                                var tempNode = pNodes[i].ChildNodes; // TODO: Fix this.  Some pages have <p> << | Main | >> </p> navigation that is being captured
                                                _log.LogInformation("Checking HtmlNode", tempNode);

                                                if (pNodes[i].HasChildNodes)
                                                {
                                                    if (pNodes[i].SelectSingleNode("//h4") != null || pNodes[i].SelectSingleNode("//h4//a") != null)
                                                    {
                                                        continue;
                                                    }
                                                }
                                                string modifiedString = Regex.Replace(pNodes[i].InnerText.Trim(), @"\r\n?|\n", string.Empty);

                                                sb.AppendLine(modifiedString);
                                            }
                                            
                                            pNodeTwo = sb.ToString();
                                        }
                                    }
                                }

                                Series series = new Series
                                {
                                    Title = title
                                };

                                if (title != previousSeriesTitle)
                                {
                                    _series.Add(series);
                                    previousSeriesTitle = title;
                                    _log.LogInformation($"Added Series { title } to the Series List.");
                                }

                                SeriesItem seriesItem = MapDictionaryToSeriesItem(DictionaryFromString(pNodeOne, '\n'));
                                seriesItem.SeriesItemImages = new List<Image>
                            {
                                new Image
                                {
                                    ImagePath = imagePath,
                                    ImageCaption = imageCaption
                                }
                            };
                                seriesItem.CollectionNumber = GetCollectionNumberFromFilename(file, title);
                                seriesItem.Description = pNodeTwo;

                                _seriesItems.Add(seriesItem);
                                _log.LogInformation($"Added { seriesItem.Title } to the SeriesItem List.");
                            }
                        }
                    }
                }
            }
            catch (IOException ie)
            {
                _log.LogError(ie.Message, ie);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message, ex);
            }
        }

        private SeriesItem MapDictionaryToSeriesItem(Dictionary<string, string> seriesItemPairs)
        {
            SeriesItem item = new SeriesItem();

            item.Title = seriesItemPairs["Title"];

            if (seriesItemPairs.ContainsKey("Distributor"))
            {
                string distName = seriesItemPairs["Distributor"];
                if (distName.Contains("Bandai Ent", StringComparison.OrdinalIgnoreCase))
                    distName = "Bandai Visual";

                Distributor distributor = _distributors.Find(x => x.DistributorName.Contains(distName, StringComparison.OrdinalIgnoreCase));
                item.Distributors = new List<Distributor>
                {
                    distributor
                };
            }

            if (seriesItemPairs.ContainsKey("Run Time"))
                item.Length = seriesItemPairs["Run Time"];

            if (seriesItemPairs.ContainsKey("Release Date"))
            {
                DateTime.TryParse(seriesItemPairs["Release Date"], out DateTime releaseDate);
                item.ReleaseDate = releaseDate;
            }

            return item;
        }

        /// <summary>
        /// Builds a <see cref="Dictionary{TKey, TValue}"/> based on the <paramref name="stringToParse"/> and <paramref name="parseByCharacter"/> passed.
        /// </summary>
        /// <param name="stringToParse"></param>
        /// <param name="parseByCharacter"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns>A <see cref="Dictionary{TKey, TValue}"/></returns>
        private Dictionary<string, string> DictionaryFromString(string stringToParse, char parseByCharacter)
        {
            if (string.IsNullOrEmpty(stringToParse))
                throw new ArgumentNullException($"{ nameof(stringToParse) } was null or string.empty.");

            Dictionary<string, string> seriesItemFields = new Dictionary<string, string>();

            string[] fields = stringToParse.Split(parseByCharacter);

            if (fields.Any())
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = fields[i].Trim();

                    if (i == 0)
                    {
                        seriesItemFields.Add("Title", fields[i]);
                    }
                    else
                    {
                        string[] pairs = fields[i].Split(':');
                        if (pairs.Length > 1 && !string.IsNullOrEmpty(pairs[1]))
                        {
                            seriesItemFields.Add(pairs[0].Trim(), pairs[1].Trim());
                        }
                    }
                }
            }

            return seriesItemFields;
        }

        /// <summary>
        /// Uses the <paramref name="title"/> and <paramref name="file"/> to get the collection number from the filename
        /// </summary>
        /// <param name="file"></param>
        /// <param name="title"></param>
        /// <returns>Returns the <see cref="Int32"/> collection number based on the file name, or zero if there is no number in the file name.</returns>
        private int GetCollectionNumberFromFilename(string file, string title)
        {
            int collectionNumber = 0;
            string filename = Path.GetFileName(file);

            // Change title to match filename: the tv show => the_tv_show
            title = title.Replace(" ", "_").ToLower();
            filename = filename.Replace(".html", string.Empty);

            if (filename.Contains(title, StringComparison.OrdinalIgnoreCase))
            {
                filename = filename.Replace(title + "_", string.Empty);
                int.TryParse(filename, out collectionNumber);
            }
            else
            {
                string temp = filename.Substring(filename.Length - 2, 2);
                int.TryParse(temp, out collectionNumber);
            }

            return collectionNumber;
        }
    }
}
