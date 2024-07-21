using System.Text;

namespace XmlParser
{
    public class ForwardOnlyParser
    {
        
        private enum ParserState
        {
            Start,
            InElement,
            InAmount
        }
        
        private StreamWriter? _streamWriter;
        private int? _filteredAmount;
        private bool _trackPaths;

        public void SetOutputPath(string outputPath)
        {
            _streamWriter?.Dispose();
            _streamWriter = new StreamWriter(outputPath, append: false);
        }

        public void Parse(string xmlString, int? filteredAmount = null, bool trackPaths = false)
        {
            if (_streamWriter == null)
            {
                throw new InvalidOperationException("Output path has not been set. Call SetOutputPath first.");
            }

            _filteredAmount = filteredAmount;
            _trackPaths = trackPaths;
            

            var pathStack = new Stack<string>();
            var currentPath = new StringBuilder();
            bool isAmountValid = false;
            ParserState state = ParserState.Start;

            for (int i = 0; i < xmlString.Length; i++)
            {
                if (xmlString[i] == '<')
                {
                    if (xmlString.Substring(i, 4) == "<!--")
                    {
                        i += 4;
                        while (xmlString.Substring(i, 3) != "-->") i++;
                        i += 2;
                        continue;
                    }

                    int start = ++i;
                    while (xmlString[i] != '>') i++;
                    string element = xmlString.Substring(start, i - start);

                    if (element.StartsWith("/"))
                    {
                        HandleEndElement(element.Substring(1), ref pathStack, ref currentPath, ref state);
                    }
                    else
                    {
                        Dictionary<string, string> attributes = ParseAttributes(element);
                        HandleStartElement(element.Split(' ')[0], attributes, ref pathStack, ref currentPath, ref state, ref isAmountValid);
                    }
                }
                else if (xmlString[i] != '<' && xmlString[i] != '>')
                {
                    int start = i;
                    while (i < xmlString.Length && xmlString[i] != '<') i++;
                    string text = xmlString.Substring(start, i - start).Trim();
                    i--;
                    if (!string.IsNullOrEmpty(text))
                    {
                        HandleText(text, ref isAmountValid, ref state);
                    }
                }
            }
            _streamWriter?.Dispose();
        }

        private Dictionary<string, string> ParseAttributes(string element)
        {
            var attributes = new Dictionary<string, string>();
            var parts = element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (part.Contains("="))
                {
                    var keyValue = part.Split(new char[] { '=' }, 2);
                    var key = keyValue[0];
                    var value = keyValue[1].Trim('"');
                    attributes[key] = value;
                }
            }

            return attributes;
        }

        private void HandleStartElement(string elementName, Dictionary<string, string> attributes,
            ref Stack<string> pathStack, ref StringBuilder currentPath, ref ParserState state,
            ref bool isAmountValid)
        {
            currentPath.Append("/").Append(elementName);
            pathStack.Push(currentPath.ToString());
            
            if (!_trackPaths && attributes.ContainsKey("id") && isAmountValid)
            {
                FireElementParsedEvent($"Order ID: {attributes["id"]}");
            }
            if (_trackPaths && attributes.ContainsKey("id") && isAmountValid)
            {
                FireElementParsedEvent($"{currentPath}/@id = \"{attributes["id"]}\"");
            }

            if (elementName == "amount")
            {
                state = ParserState.InAmount;
                isAmountValid = false;
            }
            else
            {
                state = ParserState.InElement;
            }
        }

        private void HandleEndElement(string elementName, ref Stack<string> pathStack, ref StringBuilder currentPath, ref ParserState state)
        {
            if (pathStack.Count > 0)
            {
                pathStack.Pop();
            }
            currentPath.Clear();
            if (pathStack.Count > 0)
            {
                currentPath.Append(pathStack.Peek());
            }

            state = elementName == "amount" ? ParserState.InElement : ParserState.Start;
        }

        private void HandleText(string text, ref bool isAmountValid, ref ParserState state)
        {
            text = DecodeXmlEntities(text);
            

            if (state == ParserState.InAmount && int.TryParse(text, out int amount))
            {
                isAmountValid = !_filteredAmount.HasValue || amount > _filteredAmount.Value;
                if (isAmountValid)
                {
                    FireAmountParsedEvent(text);
                }
            }
        }

        private void FireElementParsedEvent(string elementInfo)
        {
            _streamWriter?.WriteLine(elementInfo);
        }

        private void FireAmountParsedEvent(string amountValue)
        {
            _streamWriter?.WriteLine($"Amount: {amountValue}");
        }
        
        private string DecodeXmlEntities(string text)
        {
            return text.Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&amp;", "&")
                .Replace("&apos;", "'")
                .Replace("&quot;", "\"");
        }
    }
}