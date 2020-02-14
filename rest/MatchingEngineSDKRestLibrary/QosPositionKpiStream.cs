/**
 * Copyright 2018-2020 MobiledgeX, Inc. All rights and licenses reserved.
 * MobiledgeX, Inc. 156 2nd Street #408, San Francisco, CA 94105
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DistributedMatchEngine
{

  public class QosPositionKpiStream
  {
    StreamReader sr = null;
    Stream jsonObjectStream;
    DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(QosPositionKpiStreamReply));

    const int msTimeoutDefault = 15000;

    public QosPositionKpiStream(Stream jsonObjectStream, int msTimeout = msTimeoutDefault)
    {
      if (msTimeout < 0)
      {
        msTimeout = msTimeoutDefault;
      }
      this.jsonObjectStream = jsonObjectStream;
      sr = GetStreamReader(msTimeout);
    }

    public StreamReader GetStreamReader(int msTimeout)
    {
      if (jsonObjectStream.CanTimeout)
      {
        jsonObjectStream.ReadTimeout = msTimeout;
      }
      sr = new StreamReader(jsonObjectStream, true); // Detect encoding.
      return sr;
    }

    // skip '' blocks. Also skips escape chars.
    public long skipCharQuote(StreamReader sr, StringBuilder sb)
    {
      long charsRead = 0;

      int c;
      bool esc = false;
      while (sr.Peek() > -1 && !sr.EndOfStream)
      {
        c = sr.Read();
        sb.Append(Char.ConvertFromUtf32(c));
        charsRead++;

        // Not a strict check
        if (esc == true)
        {
          esc = false;
          continue;
        }
        if (c == '\\')
        {
          esc = true;
          continue;
        }
        else if (c == '\'')
        {
          return charsRead;
        }
      }

      Console.Error.WriteLine("Hit end of stream while parsing for single quote char --> ' <--");
      return charsRead;
    }

    // skip "" blocks. Also skips escape chars.
    public long skipStringQuote(StreamReader sr, StringBuilder sb)
    {
      long charsRead = 0;

      int c;
      bool esc = false;
      while (sr.Peek() > -1 && !sr.EndOfStream)
      {
        c = sr.Read();
        sb.Append(Char.ConvertFromUtf32(c));
        charsRead++;

        // Not a strict check
        if (esc == true)
        {
          esc = false;
          continue;
        }
        if (c == '\\')
        {
          esc = true;
          continue;
        }
        else if (c == '"')
        {
          return charsRead;
        }
      }

      Console.Error.WriteLine("Hit end of stream while parsing for double quote char --> \" <--");
      return charsRead;
    }

    // Very basic parser, this just chops out potential JSON strings from a stream, then hands it over
    // to a full parser that operates on complete bounded JSON strings.
    public string ParseJsonBlock()
    {
      int c = 0;
      long charsRead = 0;
      long quoteRead = 0;

      StringBuilder objsb = null;
      bool stopParsing = false;

      var s = new Stack<int>();

      while (sr.Peek() > -1 && !sr.EndOfStream)
      {
        if (stopParsing)
        {
          break;
        }

        c = sr.Read();
        charsRead++;
        string cs = Char.ConvertFromUtf32(c);

        if (objsb == null)
        {
          objsb = new StringBuilder();
        }
        objsb.Append(cs);

        switch (c)
        {
          case '{':
            s.Push(c);
            break;
          case '}':
            if (s.Peek() == '{')
            {
              s.Pop();
              if (s.Count == 0)
              {
                stopParsing = true;
                break;
              }
            }
            else
            {
              Console.Error.WriteLine("This is not valid JSON: " + c + ", position: " + charsRead);
              break;
            }
            break;
          case '[':
            s.Push(c);
            break;
          case ']':
            if (s.Peek() == '[')
            {
              s.Pop();
            }
            else
            {
              Console.Error.WriteLine("This is not valid JSON: " + c + ", position: " + charsRead);
            }
            break;
          case '\'':
            quoteRead = skipCharQuote(sr, objsb);
            charsRead += quoteRead;
            break;
          case '"':
            quoteRead = skipStringQuote(sr, objsb);
            charsRead += quoteRead;
            break;
        }
      }


      if (s.Count > 0) // Unbalanced.
      {
        Console.Error.WriteLine("Parse error. EndOfStream encountered while parsing.");
        return null;
      }

      if (s.Count == 0 && stopParsing)
      {
        return objsb.ToString(); // Parsed.
      }
      else
      {
        return null;
      }
    }

    private ReplyStatus ParseReplyStatus(string responseStr)
    {
      JsonObject jsObj = (JsonObject)JsonValue.Parse(responseStr);
      string statusStr;
      if (jsObj != null && jsObj.TryGetValue("result", out JsonValue resultValue))
      {
        statusStr = resultValue["status"];
        if (statusStr != null)
        {
          ReplyStatus replyStatus;
          try
          {
            replyStatus = (ReplyStatus)Enum.Parse(typeof(ReplyStatus), statusStr);
          }
          catch
          {
            replyStatus = ReplyStatus.RS_UNDEFINED;
          }
          return replyStatus;
        }
      }
      return ReplyStatus.RS_UNDEFINED;
    }

    public IEnumerator<QosPositionKpiReply> GetEnumerator()
    {
      while (sr.Peek() > -1 && !sr.EndOfStream)
      {
        string qprJsonStr = ParseJsonBlock();

        if (qprJsonStr != null)
        {
          byte[] byteArray = sr.CurrentEncoding.GetBytes(qprJsonStr);
          MemoryStream ms = new MemoryStream(byteArray);
          QosPositionKpiStreamReply reply = null;
          try
          {
            reply = deserializer.ReadObject(ms) as QosPositionKpiStreamReply;
            // Re-parse if still on default value.
            reply.result.status = reply.result.status == ReplyStatus.RS_UNDEFINED ? ParseReplyStatus(qprJsonStr) : reply.result.status;
          }
          catch (Exception e)
          {
            Console.Error.WriteLine(e.StackTrace);
            Console.Error.WriteLine(reply.error);
          }

          // Could be null
          yield return reply.result;
        }
      }
    }

    public void Dispose()
    {
      jsonObjectStream.Dispose();
      sr.Dispose();
    }

  }

}
