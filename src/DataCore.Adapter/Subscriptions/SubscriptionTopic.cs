// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace DataCore.Adapter.Subscriptions {
    public readonly struct SubscriptionTopic : IEquatable<SubscriptionTopic> {

        private static readonly char[] s_defaultLevelSeparators = { SubscriptionTopicFilterComparer.LevelSeparator };

        public string Topic { get; }

        public ulong TopicHash { get; }

        public ulong TopicHashMask { get; }

        public bool TopicContainsWildcard { get; }


        public SubscriptionTopic(string topic, SubscriptionManagerOptions? options = null) {
            CalculateHash(
                topic ?? throw new ArgumentNullException(nameof(topic)),
                options,
                out var resultHash,
                out var resultHashMask,
                out var resultHasWildcard
            );

            Topic = topic;
            TopicHash = resultHash;
            TopicHashMask = resultHashMask;
            TopicContainsWildcard = resultHasWildcard;
        }


        public override int GetHashCode() {
            return HashCode.Combine(Topic, TopicHash, TopicHashMask, TopicContainsWildcard);
        }


        public override bool Equals(object obj) {
            return obj is SubscriptionTopic subscriptionTopic
                ? Equals(subscriptionTopic)
                : false;
        }


        public bool Equals(SubscriptionTopic other) {
            return
                TopicHash == other.TopicHash &&
                TopicHashMask == other.TopicHashMask &&
                Topic == other.Topic && 
                TopicContainsWildcard == other.TopicContainsWildcard;
        }


        private static void CalculateHash(string topic, SubscriptionManagerOptions? options, out ulong resultHash, out ulong resultHashMask, out bool resultHasWildcard) {
            var levelSeparators = options?.TopicLevelSeparators ?? s_defaultLevelSeparators;
            var singleLevelWildcard = options?.SingleLevelWildcard ?? SubscriptionTopicFilterComparer.SingleLevelWildcard;
            var multiLevelWildcard = options?.MultiLevelWildcard ?? SubscriptionTopicFilterComparer.MultiLevelWildcard;
            var wildcardsEnabled = options?.EnableWildcardSubscriptions ?? true;

            // calculate topic hash
            ulong hash = 0;
            ulong hashMaskInverted = 0;
            ulong levelBitMask = 0;
            ulong fillLevelBitMask = 0;
            var hasWildcard = false;
            byte checkSum = 0;
            var level = 0;

            var i = 0;
            while (i < topic.Length) {
                var c = topic[i];
                if (levelSeparators.Any(x => x == c)) {
                    // done with this level
                    hash <<= 8;
                    hash |= checkSum;
                    hashMaskInverted <<= 8;
                    hashMaskInverted |= levelBitMask;
                    checkSum = 0;
                    levelBitMask = 0;
                    ++level;
                    if (level >= 8) {
                        break;
                    }
                }
                else if (wildcardsEnabled && c == singleLevelWildcard) {
                    levelBitMask = 0xff;
                    hasWildcard = true;
                }
                else if (wildcardsEnabled && c == multiLevelWildcard) {
                    // checksum is zero for a valid topic
                    levelBitMask = 0xff;
                    // fill rest with this fillLevelBitMask
                    fillLevelBitMask = 0xff;
                    hasWildcard = true;
                    break;
                }
                else {
                    // The checksum should be designed to reduce the hash bucket depth for the expected
                    // fairly regularly named MQTT topics that don't differ much,
                    // i.e. "room1/sensor1"
                    //      "room1/sensor2"
                    //      "room1/sensor3"
                    // etc.
                    if ((c & 1) == 0) {
                        checkSum += (byte) c;
                    }
                    else {
                        checkSum ^= (byte) (c >> 1);
                    }
                }

                ++i;
            }

            // Shift hash left and leave zeroes to fill ulong
            if (level < 8) {
                hash <<= 8;
                hash |= checkSum;
                hashMaskInverted <<= 8;
                hashMaskInverted |= levelBitMask;
                ++level;
                while (level < 8) {
                    hash <<= 8;
                    hashMaskInverted <<= 8;
                    hashMaskInverted |= fillLevelBitMask;
                    ++level;
                }
            }

            if (!hasWildcard && wildcardsEnabled) {
                while (i < topic.Length) {
                    var c = topic[i];
                    if (c == singleLevelWildcard || c == multiLevelWildcard) {
                        hasWildcard = true;
                        break;
                    }

                    ++i;
                }
            }

            resultHash = hash;
            resultHashMask = ~hashMaskInverted;
            resultHasWildcard = hasWildcard;
        }

    }
}
