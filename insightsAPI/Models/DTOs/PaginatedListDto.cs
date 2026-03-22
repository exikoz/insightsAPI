using System.Text.Json.Serialization;

namespace insightsAPI.Models.DTOs
{
    /// <summary>
    /// A generic reusable structure for returning paginated lists
    /// </summary>
    /// <typeparam name="T">The type of object contained in the list</typeparam>
    public class PaginatedListDto<T>
    {
        /// <summary>
        /// The actual data objects for the current page
        /// </summary>
        [JsonPropertyName("items")]
        public required List<T> Items { get; set; }

        /// <summary>
        /// Current page number (1-indexed)
        /// </summary>
        /// <example>1</example>
        [JsonPropertyName("page")]
        public int Page { get; set; }

        /// <summary>
        /// Number of objects configured per page
        /// </summary>
        /// <example>20</example>
        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of objects matching the search across all pages
        /// </summary>
        /// <example>1584</example>
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages available
        /// </summary>
        /// <example>80</example>
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }
}
