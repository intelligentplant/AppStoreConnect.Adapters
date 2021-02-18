using System;
using System.ComponentModel.DataAnnotations;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter {

    /// <summary>
    /// Base class for adapter implementations that do not require a custom <see cref="AdapterOptions"/> 
    /// type.
    /// </summary>
    /// <seealso cref="AdapterBase{TAdapterOptions}"/>
    public abstract class AdapterBase : AdapterBase<AdapterOptions> {

        /// <summary>
        /// Creates a new <see cref="AdapterBase"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="name">
        ///   The adapter display name.
        /// </param>
        /// <param name="description">
        ///   The adapter description.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        ///  <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is longer than <see cref="AdapterConstants.MaxIdLength"/>.
        /// </exception>
        /// <param name="logger">
        ///   The logger for the adapter. Can be <see langword="null"/>.
        /// </param>
        /// <exception cref="ValidationException">
        ///   The <paramref name="name"/> is longer than <see cref="AdapterConstants.MaxNameLength"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   The <paramref name="description"/> is longer than <see cref="AdapterConstants.MaxDescriptionLength"/>.
        /// </exception>
        protected AdapterBase(string id, string? name, string? description, IBackgroundTaskService? backgroundTaskService, ILogger? logger)
            : this(id, Microsoft.Extensions.Options.Options.Create(new AdapterOptions() { Name = name, Description = description }), backgroundTaskService, logger) { }


        /// <summary>
        /// Creates a new <see cref="AdapterBase"/> object.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        ///  <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is longer than <see cref="AdapterConstants.MaxIdLength"/>.
        /// </exception>
        /// <param name="logger">
        ///   The logger for the adapter. Can be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   The <paramref name="options"/> are not valid.
        /// </exception>
        protected AdapterBase(string id, AdapterOptions options, IBackgroundTaskService? backgroundTaskService, ILogger? logger)
            : this(id, Microsoft.Extensions.Options.Options.Create(options ?? throw new ArgumentNullException(nameof(options))), backgroundTaskService, logger) { }


        /// <summary>
        /// Creates a new <see cref="AdapterBase{TAdapterOptions}"/> object that receives its 
        /// configuration from an <see cref="IOptions{TOptions}"/>.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="options">
        ///   The adapter options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger for the adapter. Can be <see langword="null"/>.
        /// </param>
        ///  <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is longer than <see cref="AdapterConstants.MaxIdLength"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The value of the <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   The value of the <paramref name="options"/> is not valid.
        /// </exception>
        protected AdapterBase(string id, IOptions<AdapterOptions> options, IBackgroundTaskService? backgroundTaskService, ILogger? logger)
            : base(id, options, backgroundTaskService, logger) { }


        /// <summary>
        /// Creates a new <see cref="AdapterBase{TAdapterOptions}"/> object that receives its 
        /// configuration from an <see cref="IOptionsMonitor{TOptions}"/> and can monitor for 
        /// configuration changes.
        /// </summary>
        /// <param name="id">
        ///   The adapter ID.
        /// </param>
        /// <param name="optionsMonitor">
        ///   The monitor for the adapter's options type. The <see cref="IOptionsMonitor{TOptions}"/> 
        ///   key used is the supplied <paramref name="id"/>.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> that the adapter can use to run background 
        ///   operations. Specify <see langword="null"/> to use the default implementation.
        /// </param>
        /// <param name="logger">
        ///   The logger factory for the adapter. Can be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="id"/> is longer than <see cref="AdapterConstants.MaxIdLength"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="optionsMonitor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="optionsMonitor"/> does not contain an entry that can be used with this adapter.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   The initial options retrieved from <paramref name="optionsMonitor"/> are not valid.
        /// </exception>
        /// <remarks>
        ///   Note to implementers: override the <see cref="AdapterBase{TAdapterOptions}.OnOptionsChange"/> 
        ///   method on your adapter implementation to receive notifications of options changes 
        ///   received from the <paramref name="optionsMonitor"/>.
        /// </remarks>
        protected AdapterBase(string id, IOptionsMonitor<AdapterOptions> optionsMonitor, IBackgroundTaskService? backgroundTaskService, ILogger? logger)
            : base(id, optionsMonitor, backgroundTaskService, logger) { }

    }
}
