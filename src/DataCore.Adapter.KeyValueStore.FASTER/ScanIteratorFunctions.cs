using System;
using System.Threading.Channels;

using FASTER.core;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// <see cref="IScanIteratorFunctions{Key, Value}"/> implementation that allows us to use 
    /// FASTER's "push" key iteration: https://microsoft.github.io/FASTER/docs/fasterkv-basics/#key-iteration
    /// </summary>
    internal struct ScanIteratorFunctions : IScanIteratorFunctions<SpanByte, SpanByte> {

        /// <summary>
        /// The channel to publish keys to as they are iterated over.
        /// </summary>
        private readonly Channel<FasterRecord> _channel;

        /// <summary>
        /// Specifies if the iterator should include record values.
        /// </summary>
        private readonly bool _includeValues;

        /// <summary>
        /// The channel reader.
        /// </summary>
        public ChannelReader<FasterRecord> Reader => _channel?.Reader!;


        /// <summary>
        /// Creates a new <see cref="ScanIteratorFunctions"/> instance.
        /// </summary>
        /// <param name="includeValues">
        ///   Specifies if the iterator should include record values.
        /// </param>
        internal ScanIteratorFunctions(bool includeValues) {
            _includeValues = includeValues;
            _channel = Channel.CreateUnbounded<FasterRecord>(new UnboundedChannelOptions() {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true
            });
        }


        /// <inheritdoc/>
        public bool OnStart(long beginAddress, long endAddress) => true;


        /// <inheritdoc/>
        public bool SingleReader(ref SpanByte key, ref SpanByte value, RecordMetadata recordMetadata, long numberOfRecords) {
            return _channel.Writer.TryWrite(new FasterRecord(key.ToByteArray(), recordMetadata, false, _includeValues ? new ReadOnlyMemory<byte>(value.ToByteArray()) : default));
        }


        /// <inheritdoc/>
        public bool ConcurrentReader(ref SpanByte key, ref SpanByte value, RecordMetadata recordMetadata, long numberOfRecords) {
            return _channel.Writer.TryWrite(new FasterRecord(key.ToByteArray(), recordMetadata, true, _includeValues ? new ReadOnlyMemory<byte>(value.ToByteArray()) : default));
        }


        /// <inheritdoc/>
        public void OnStop(bool completed, long numberOfRecords) {
            _channel.Writer.TryComplete();
        }


        /// <inheritdoc/>
        public void OnException(Exception exception, long numberOfRecords) {
            _channel.Writer.TryComplete(exception);
        }

    }

}
