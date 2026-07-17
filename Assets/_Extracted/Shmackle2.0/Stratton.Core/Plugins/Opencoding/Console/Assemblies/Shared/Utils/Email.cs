using System.Collections.Generic;
using System.IO;

namespace Opencoding.Shared.Utils
{
	/// <summary>
	/// This data structure represents an email. It can be sent using the NativeMethods/NativeMethodsInterface class.
	/// </summary>
	public class Email
	{
		public class Attachment
		{
			public string Filename { get; private set; }
			public string MimeType { get; private set; }
			public byte[] Data { get; private set; }

			public Attachment(string filename, string mimeType, byte[] data)
			{
				Data = data;
				Filename = filename;
				MimeType = mimeType;
			}

			public Attachment(string path, string mimeType)
			{
				Data = File.ReadAllBytes(path);
				Filename = Path.GetFileName(path);
				MimeType = mimeType;
			}
		}

		public string Message { get; set; }

		public string ToAddress { get; set; }

		public string Subject { get; set; }

		public bool IsHTML { get; set; }

		public List<Attachment> Attachments { get; set; }

		public Email()
		{
			Attachments = new List<Attachment>();
		}
	}
}