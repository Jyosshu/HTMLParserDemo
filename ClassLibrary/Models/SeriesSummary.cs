using System.Collections.Generic;

namespace ClassLibrary
{
    public class SeriesSummary
    {
        public int SeriesId { get; set; }
        public string Title { get; set; }
        // TODO: Genre?

        public List<CreatorAuthor> CreatorAuthors { get; set; }
        public List<SeriesItem> SeriesItems { get; set; }
    }
}
