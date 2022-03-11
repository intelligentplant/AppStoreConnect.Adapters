using System;
using System.ComponentModel.DataAnnotations;

using Json.Schema;
using Json.Schema.Generation;

namespace DataCore.Adapter.Json.Schema {

    /// <summary>
    /// Adds a <c>format</c> keyword to a schema for a <see cref="TimeSpan"/>.
    /// </summary>
    public class TimeSpanFormatIntent : ISchemaKeywordIntent {

        /// <inheritdoc/>
        public void Apply(JsonSchemaBuilder builder) {
            builder.Format(new Format("timespan"));
        }
    }


    /// <summary>
    /// Adds a <c>format</c> keyword to a schema for a given <see cref="DataType"/>.
    /// </summary>
    public class DataTypeFormatIntent : ISchemaKeywordIntent {

        /// <summary>
        /// The <see cref="DataType"/>.
        /// </summary>
        private readonly DataType _dataType;


        /// <summary>
        /// Creates a new <see cref="DataTypeFormatIntent"/> object.
        /// </summary>
        /// <param name="dataType">
        ///   The <see cref="DataType"/>.  
        /// </param>
        public DataTypeFormatIntent(DataType dataType) {
            _dataType = dataType;
        }


        /// <inheritdoc/>
        public void Apply(JsonSchemaBuilder builder) {
            switch (_dataType) {
                case DataType.Date:
                    builder.Format(new Format("date"));
                    break;
                case DataType.DateTime:
                    builder.Format(new Format("date-time"));
                    break;
                case DataType.Duration:
                    builder.Format(new Format("duration"));
                    break;
                case DataType.EmailAddress:
                    builder.Format(new Format("email"));
                    break;
                case DataType.ImageUrl:
                    builder.Format(new Format("uri"));
                    break;
                case DataType.MultilineText:
                    builder.Format(new Format("multiline-text"));
                    break;
                case DataType.Password:
                    builder.Format(new Format("password"));
                    break;
                case DataType.Text:
                    builder.Format(new Format("text"));
                    break;
                case DataType.Time:
                    builder.Format(new Format("time"));
                    break;
                case DataType.Url:
                    builder.Format(new Format("uri"));
                    break;

            }
        }
    }


    /// <summary>
    /// Adds a <c>title</c> and/or <c>description</c> keyword to a schema.
    /// </summary>
    public class DisplayIntent : ISchemaKeywordIntent {

        /// <summary>
        /// The title.
        /// </summary>
        private readonly string? _title;

        /// <summary>
        /// The description.
        /// </summary>
        private readonly string? _description;


        /// <summary>
        /// Creates a new <see cref="DisplayIntent"/> object.
        /// </summary>
        /// <param name="name">
        ///   The schema title.
        /// </param>
        /// <param name="description">
        ///   The schema description.
        /// </param>
        public DisplayIntent(string? name, string? description) {
            _title = name;
            _description = description;
        }


        /// <inheritdoc/>
        public void Apply(JsonSchemaBuilder builder) {
            if (_title != null) {
                builder.Title(_title);
            }
            if (_description != null) {
                builder.Description(_description);
            }
        }

    }


    /// <summary>
    /// Adds a <c>maxLength</c> keyword to a schema.
    /// </summary>
    public class MaxLengthIntent : ISchemaKeywordIntent {

        /// <summary>
        /// The limit.
        /// </summary>
        private readonly int _limit;


        /// <summary>
        /// Creates a new <see cref="MaxLengthIntent"/> object.
        /// </summary>
        /// <param name="limit">
        ///   The limit.
        /// </param>
        public MaxLengthIntent(int limit) {
            _limit = limit < 0 ? 0 : limit;
        }


        /// <inheritdoc/>
        public void Apply(JsonSchemaBuilder builder) {
            builder.MaxLength((uint) _limit);
        }

    }


    /// <summary>
    /// Adds a <c>minLength</c> keyword to a schema.
    /// </summary>
    public class MinLengthIntent : ISchemaKeywordIntent {

        /// <summary>
        /// The limit.
        /// </summary>
        private readonly int _limit;


        /// <summary>
        /// Creates a new <see cref="MinLengthIntent"/> object.
        /// </summary>
        /// <param name="limit">
        ///   The limit.
        /// </param>
        public MinLengthIntent(int limit) {
            _limit = limit < 0 ? 0 : limit;
        }


        /// <inheritdoc/>
        public void Apply(JsonSchemaBuilder builder) {
            builder.MinLength((uint) _limit);
        }

    }


    /// <summary>
    /// Adds a <c>minimum</c> and/or <c>maximum</c> keyword to a schema.
    /// </summary>
    public class RangeIntent : ISchemaKeywordIntent {

        /// <summary>
        /// The minimum value.
        /// </summary>
        private readonly decimal? _lower;

        /// <summary>
        /// The maximum value.
        /// </summary>
        private readonly decimal? _upper;


        /// <summary>
        /// Creates a new <see cref="RangeIntent"/> object.
        /// </summary>
        /// <param name="lower">
        ///   The lower limit.
        /// </param>
        /// <param name="upper">
        ///   The upper limit.
        /// </param>
        public RangeIntent(decimal? lower, decimal? upper) {
            _lower = lower;
            _upper = upper;
        }


        /// <inheritdoc/>
        public void Apply(JsonSchemaBuilder builder) {
            if (_lower.HasValue) {
                builder.Minimum(_lower.Value);
            }
            if (_upper.HasValue) {
                builder.Maximum(_upper.Value);
            }
        }
    }

}
