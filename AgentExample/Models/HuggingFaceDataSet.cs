using System.Text.Json.Serialization;

namespace AgentExample.Models
{
    public class HuggingFaceDataSet
    {
        [JsonPropertyName("features")]
        public List<FeatureElement>? Features { get; set; }

        [JsonPropertyName("rows")]
        public List<RowElement>? Rows { get; set; }

        [JsonPropertyName("num_rows_total")]
        public int NumRowsTotal { get; set; }

        [JsonPropertyName("num_rows_per_page")]
        public int NumRowsPerPage { get; set; }

        [JsonPropertyName("partial")]
        public bool Partial { get; set; }
    }

    public class FeatureElement
    {
        [JsonPropertyName("feature_idx")]
        public int FeatureIdx { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public TypeClass? Type { get; set; }
    }

    public class TypeClass
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("dtype")]
        public string? Dtype { get; set; }

        [JsonPropertyName("_type")]
        public string? Type { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("feature")]
        public TypeFeature? Feature { get; set; }
    }

    public class TypeFeature
    {
        [JsonPropertyName("dtype")]
        public string? Dtype { get; set; }

        [JsonPropertyName("_type")]
        public string? Type { get; set; }
    }

    public class RowElement
    {
        [JsonPropertyName("row_idx")]
        public int RowIdx { get; set; }

        [JsonPropertyName("row")]
        public RowRow? Row { get; set; }

        [JsonPropertyName("truncated_cells")]
        public List<object> TruncatedCells { get; set; } = [];
    }

    public class RowRow
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("embedding")]
        public List<double> Embedding { get; set; } = [];

        [JsonPropertyName("__index_level_0__")]
        public int IndexLevel0__ { get; set; }
    }
}
