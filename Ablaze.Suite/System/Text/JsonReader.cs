using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Text {
	using JsonPair = KeyValuePair<string, JsonObject>;

	internal class JsonReader : BaseReader {
		public JsonReader(TextReader reader, IJsonReader jsonReader, bool disposeStream)
		  : base(reader, jsonReader, disposeStream) {
		}

		public JsonObject Read() {
			JsonObject v = ReadCore();
			SkipWhite();
			if (ReadChar() >= 0)
				throw ParseError("Extra characters in JSON input");
			return v;
		}

		JsonObject ReadCore() {
			int c = SkipPeekChar();
			if (c < 0)
				throw ParseError("Incomplete JSON input");
			switch (c) {
				case '[':
					ReadChar();
					if (SkipPeekChar() == ']') {
						ReadChar();
						return new JsonArray();
					}
					var list = new List<JsonObject>();
					for (int i = 0; ; i++) {
						if (HasReader)
							Reader.Index(i);
						var value = ReadCore();
						if (HasReader)
							Reader.Value(value);
						list.Add(value);
						c = SkipPeekChar();
						if (c != ',')
							break;
						ReadChar();
					}
					if (ReadChar() != ']')
						throw ParseError("Array must end with ']'");
					return new JsonArray(list);
				case '{':
					ReadChar();
					if (SkipPeekChar() == '}') {
						ReadChar();
						return new JsonObject();
					}
					var obj = new List<JsonPair>();
					for (;;) {
						if (SkipPeekChar() == '}') {
							ReadChar();
							break;
						}
						if (PeekChar() != '"')
							throw ParseError("Invalid JSON string literal format");
						string name = ReadStringLiteral(null);
						SkipWhite();
						Expect(':');
						SkipWhite();
						if (HasReader)
							Reader.Key(name);
						var value = ReadCore();
						if (HasReader)
							Reader.Value(value);
						obj.Add(new JsonPair(name, value));
						SkipWhite();
						c = ReadChar();
						if (c == '}')
							break;
						//if (c==',') continue;
					}
					return new JsonObject(obj);
				case 't':
					Expect("true");
					return true;
				case 'f':
					Expect("false");
					return false;
				case 'n':
					Expect("null");
					return (JsonObject) null;
				case '"':
					return ReadStringLiteral(null);
				default:
					if (c >= '0' && c <= '9' || c == '-')
						return ReadNumericLiteral();
					else
						throw ParseError(String.Format("Unexpected character '{0}'", (char) c));
			}
		}
	}
}