
```
[example]
  Name: Example Adapter
  Description: Example adapter, built using the tutorial on GitHub
  Properties:
    - Startup Time = 2020-03-15T15:54:55Z
  Features:
    - IHealthCheck
    - IReadTagValuesAtTimes
    - IReadProcessedTagValues
    - IReadSnapshotTagValues
    - IReadPlotTagValues
    - ITagSearch
    - ISnapshotTagValuePush
    - ITagInfo
    - IReadRawTagValues

  Supported Aggregations:
    - AVG
      - Name: Average
      - Description: Average value calculated over a fixed sample interval.
    - COUNT
      - Name: Count
      - Description: The number of raw samples that have been recorded for the tag over the sample period.
    - INTERP
      - Name: Interpolated
      - Description: Interpolates a value at each sample interval based on the raw values on either side of the sample time for the interval.
    - MAX
      - Name: Maximum
      - Description: Maximum value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the maximum value occurred at.
    - MIN
      - Name: Minimum
      - Description: Minimum value calculated over a fixed sample interval. The calculated value contains the actual timestamp that the minimum value occurred at.
    - PERCENTBAD
      - Name: Percent Bad
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have bad-quality status.
    - PERCENTGOOD
      - Name: Percent Good
      - Description: At each interval in a time range, calculates the percentage of raw samples in that interval that have good-quality status.
    - RANGE
      - Name: Range
      - Description: The difference between the minimum value and maximum value over the sample period.

[Tag Details]
  Name: RandomValue_1
  ID: 1
  Description: A tag that returns a random value
  Properties:
    - MinValue = 50
    - MaxValue = 200

  Raw Values (15:53:55 - 15:54:55 UTC):
    - 130.31675304766594 @ 2020-03-15T15:53:55.6762442Z [Good Quality]
    - 157.90516750789487 @ 2020-03-15T15:54:01.6762442Z [Good Quality]
    - 117.29246860709623 @ 2020-03-15T15:54:05.6762442Z [Good Quality]
    - 149.94361740534362 @ 2020-03-15T15:54:13.6762442Z [Good Quality]
    - 81.92332935609079 @ 2020-03-15T15:54:20.6762442Z [Bad Quality]
    - 169.40191966453656 @ 2020-03-15T15:54:25.6762442Z [Good Quality]
    - 83.05459417544985 @ 2020-03-15T15:54:29.6762442Z [Bad Quality]
    - 57.319277691337874 @ 2020-03-15T15:54:34.6762442Z [Bad Quality]
    - 177.70466079362885 @ 2020-03-15T15:54:41.6762442Z [Bad Quality]
    - 186.49493497167478 @ 2020-03-15T15:54:47.6762442Z [Bad Quality]
    - 131.5988481191913 @ 2020-03-15T15:54:54.6762442Z [Good Quality]

  Average Values (00:00:20 sample interval):
    - 117.29246860709623 @ 2020-03-15T15:53:55.6762442Z [Good Quality]
    - 57.319277691337874 @ 2020-03-15T15:54:15.6762442Z [Bad Quality]
    - 131.5988481191913 @ 2020-03-15T15:54:35.6762442Z [Bad Quality]

  Count Values (00:00:20 sample interval):
    - 4 @ 2020-03-15T15:53:55.6762442Z [Good Quality]
    - 4 @ 2020-03-15T15:54:15.6762442Z [Good Quality]
    - 3 @ 2020-03-15T15:54:35.6762442Z [Good Quality]

  Interpolated Values (00:00:20 sample interval):
    - 130.31675304766594 @ 2020-03-15T15:53:55.6762442Z [Good Quality]
    - 130.50924939127137 @ 2020-03-15T15:54:15.6762442Z [Bad Quality]
    - 74.517189563093723 @ 2020-03-15T15:54:35.6762442Z [Bad Quality]
    - 123.75654999740794 @ 2020-03-15T15:54:55.6762442Z [Bad Quality]

  Maximum Values (00:00:20 sample interval):
    - 157.90516750789487 @ 2020-03-15T15:53:55.6762442Z [Good Quality]
    - 169.40191966453656 @ 2020-03-15T15:54:15.6762442Z [Bad Quality]
    - 186.49493497167478 @ 2020-03-15T15:54:35.6762442Z [Bad Quality]

  Minimum Values (00:00:20 sample interval):
    - 117.29246860709623 @ 2020-03-15T15:53:55.6762442Z [Good Quality]
    - 57.319277691337874 @ 2020-03-15T15:54:15.6762442Z [Bad Quality]
    - 131.5988481191913 @ 2020-03-15T15:54:35.6762442Z [Bad Quality]

  Percent Bad Values (00:00:20 sample interval):
    - 0 % @ 2020-03-15T15:53:55.6762442Z [Good Quality]
    - 75 % @ 2020-03-15T15:54:15.6762442Z [Good Quality]
    - 66.666666666666657 % @ 2020-03-15T15:54:35.6762442Z [Good Quality]

  Percent Good Values (00:00:20 sample interval):
    - 100 % @ 2020-03-15T15:53:55.6762442Z [Good Quality]
    - 25 % @ 2020-03-15T15:54:15.6762442Z [Good Quality]
    - 33.333333333333329 % @ 2020-03-15T15:54:35.6762442Z [Good Quality]

  Range Values (00:00:20 sample interval):
    - 117.29246860709623 @ 2020-03-15T15:53:55.6762442Z [Good Quality]
    - 57.319277691337874 @ 2020-03-15T15:54:15.6762442Z [Bad Quality]
    - 131.5988481191913 @ 2020-03-15T15:54:35.6762442Z [Bad Quality]
```
