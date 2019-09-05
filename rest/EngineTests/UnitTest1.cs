using NUnit.Framework;
using DistributedMatchEngine;
using System.IO;
using System.Text;
using System;

namespace Tests
{
  public class Tests
  {
    [SetUp]
    public void Setup()
    {
    }

    private MemoryStream getMemoryStream(string jsonStr)
    {
      var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonStr));
      return ms;
    }

    /**
     * Basic equivalence tests for the QosPositionKpiStream internal JSON block parser.
     */
    [Test]
    public void Test1()
    {
      QosPositionKpiStream streamParser;
      var js1 = "{ 'foo' = 1 }";
      streamParser = new QosPositionKpiStream(getMemoryStream(js1));

      string parsed;
      parsed = streamParser.ParseJsonBlock();
      Assert.AreEqual(js1, parsed);

      js1 = @"{ [{""foo"": ""2""}, {'bar' = 3}, { 'doot': '$2' }] }";
      streamParser = new QosPositionKpiStream(getMemoryStream(js1));
      parsed = streamParser.ParseJsonBlock();
      Assert.AreEqual(js1, parsed);

      var js2 = @"{ [{""foo"": ""2""}, {'bar' = 3}, { 'doot': '$2' }] }{ [{""foo"": ""2""}, {'bar' = 3}, { 'doot': '$2' }] }";
      streamParser = new QosPositionKpiStream(getMemoryStream(js2));

      for (int i = 0; i < 2; i++)
      {
        parsed = streamParser.ParseJsonBlock();
        Assert.AreEqual(js1, parsed);
      }
      streamParser.Dispose();
    }

    /**
     * Basic equivalence tests for the QosPositionKpiStream internal JSON block parser.
     * An escape char.
     */
    [Test]
    public void TestQosPositionStreamEscape()
    {
      QosPositionKpiStream streamParser;
      var js1 = @"{ [{""foo\u005C"": ""2""}, {'bar' = 3}, { 'doot': '$2' }] }";

      string parsed;
      streamParser = new QosPositionKpiStream(getMemoryStream(js1));
      parsed = streamParser.ParseJsonBlock();
      Assert.AreEqual(js1, parsed);
      streamParser.Dispose();
    }

    /**
     * Basic equivalence tests for the QosPositionKpiStream internal JSON block parser.
     */
    [Test]
    public void TestQosPositionStream()
    {
      QosPositionKpiStream streamParser;
      string parsed;
      string js1 = @"{
 ""result"": {
  ""ver"": 0,
  ""status"": ""RS_SUCCESS"",
  ""position_results"": [
   {
    ""positionid"": ""1"",
    ""gps_location"": {
     ""latitude"": 50.11729018260935,
     ""longitude"": 8.576783680147576,
     ""horizontal_accuracy"": 0,
     ""vertical_accuracy"": 0,
     ""altitude"": 0,
     ""course"": 0,
     ""speed"": 0,
     ""timestamp"": {
      ""seconds"": ""63703198734"",
      ""nanos"": 863000000
     }
},
    ""dluserthroughput_min"": 0,
    ""dluserthroughput_avg"": 31.561584,
    ""dluserthroughput_max"": 121.52567,
    ""uluserthroughput_min"": 0,
    ""uluserthroughput_avg"": 18.889288,
    ""uluserthroughput_max"": 51.594624,
    ""latency_min"": 47.231102,
    ""latency_avg"": 0,
    ""latency_max"": 0
   },
   {
    ""positionid"": ""2"",
    ""gps_location"": {
     ""latitude"": 50.124580365218705,
     ""longitude"": 8.571467360295152,
     ""horizontal_accuracy"": 0,
     ""vertical_accuracy"": 0,
     ""altitude"": 0,
     ""course"": 0,
     ""speed"": 0,
     ""timestamp"": {
      ""seconds"": ""63703198734"",
      ""nanos"": 863000000
     }
    },
    ""dluserthroughput_min"": 0,
    ""dluserthroughput_avg"": 41.597435,
    ""dluserthroughput_max"": 99.60402,
    ""uluserthroughput_min"": 0,
    ""uluserthroughput_avg"": 12.110276,
    ""uluserthroughput_max"": 37.700558,
    ""latency_min"": 47.231102,
    ""latency_avg"": 0,
    ""latency_max"": 0
   }
  ]
 }
}";
      streamParser = new QosPositionKpiStream(getMemoryStream(js1));
      parsed = streamParser.ParseJsonBlock();

      // Light existance pass:
      streamParser = new QosPositionKpiStream(getMemoryStream(js1), 15000);
      foreach (var reply in streamParser)
      {
        Assert.AreEqual(ReplyStatus.RS_SUCCESS, reply.status);
        Assert.AreEqual(2, reply.position_results.Length);
        Assert.AreEqual(50.11729018260935, reply.position_results[0].gps_location.latitude);
        Assert.AreEqual(8.576783680147576, reply.position_results[0].gps_location.longitude);
        Assert.AreEqual(47.231102f, reply.position_results[0].latency_min); // Not "very" exact.
      }
      Assert.AreEqual(js1, parsed);
      streamParser.Dispose();
    }
  }
}