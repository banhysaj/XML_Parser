using System;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlParser
{
    public class CustomXmlReader : IDisposable
    {
        private readonly StringReader _stringReader;
        private string _xml;
        private int _position;
        private bool _isDisposed;

        public string? CurrentElement { get; private set; }
        public string? CurrentValue { get; private set; }
        public string? CurrentElementEnd { get; private set; }

        public CustomXmlReader(StringReader stringReader)
        {
            _stringReader = stringReader;
            _xml = _stringReader.ReadToEnd();
            _position = 0;
        }

        public bool Read()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CustomXmlReader));

            if (_position >= _xml.Length)
                return false;

            // Reset values
            CurrentElement = null;
            CurrentValue = null;
            CurrentElementEnd = null;

            var elementPattern = @"<(?<element>\w+)([^>]*)>";
            var valuePattern = @"(?<value>(?<=^|>)\s*[^<]+(?=<|$))";
            var endElementPattern = @"</(?<element>\w+)\s*>";

            // Match element
            var elementMatch = Regex.Match(_xml.Substring(_position), elementPattern);
            if (elementMatch.Success)
            {
                CurrentElement = elementMatch.Groups["element"].Value;
                _position += elementMatch.Index + elementMatch.Length;
                return true;
            }

            // Match end element
            var endElementMatch = Regex.Match(_xml.Substring(_position), endElementPattern);
            if (endElementMatch.Success)
            {
                CurrentElementEnd = endElementMatch.Groups["element"].Value;
                _position += endElementMatch.Index + endElementMatch.Length;
                return true;
            }

            // Match value
            var valueMatch = Regex.Match(_xml.Substring(_position), valuePattern);
            if (valueMatch.Success)
            {
                CurrentValue = valueMatch.Groups["value"].Value.Trim();
                _position += valueMatch.Index + valueMatch.Length;
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _stringReader.Dispose();
                _isDisposed = true;
            }
        }
    }
}
