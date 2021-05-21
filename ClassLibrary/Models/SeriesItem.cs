using System;
using System.Collections.Generic;

namespace ClassLibrary
{
    public class SeriesItem
    {
        public int SeriesItemId { get; set; }
        
        public string Title { get; set; }

        public string Description { get; set; }
        
        public List<Image> SeriesItemImages { get; set; }

        public List<ProductionStudio> ProductionStudios { get; set; }
                
        public List<Distributor> Distributors { get; set; }

        public List<CreatorAuthor> CreatorAuthors { get; set; }

        public string Length { get; set; }
                
        public Format Format { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public int CollectionNumber { get; set; }
    }
}
