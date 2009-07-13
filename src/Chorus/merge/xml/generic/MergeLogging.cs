using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace Chorus.merge.xml.generic
{
	public interface IMergeEventListener
	{
		void ConflictOccurred(IConflict conflict);

		void ChangeOccurred(IChangeReport change);

		/// <summary>
		/// In order to be able to store in the conflict enough information to later retrieve the conflicting
		/// data, someone must call this when new element levels were reached.
		/// Then when a conflict occurs, the listener pushes this context into the conflict and (at least
		/// in the case of the xmllistener as of june2009) writes out the conflict with this context in the
		/// xml record of the conflict.  Later, a UI handling conflicts can retrieve this info in order
		/// to reconstruct exact what and where the conflict was.
		/// </summary>
		/// <param name="context">an xpath, line number, whatever works for reconstructing the situation at a later date</param>
		void EnteringContext(string context);
	}

	public class NullMergeEventListener : IMergeEventListener
	{
		public void ConflictOccurred(IConflict conflict)
		{

		}

		public void ChangeOccurred(IChangeReport change)
		{

		}

		public void EnteringContext(string context)
		{

		}
	}

	public class DispatchingMergeEventListener : IMergeEventListener
	{
		private List<IMergeEventListener> _listeners = new List<IMergeEventListener>();

		public void AddEventListener(IMergeEventListener listener)
		{
			_listeners.Add(listener);
		}

		public void ConflictOccurred(IConflict conflict)
		{
			foreach (IMergeEventListener listener in _listeners)
			{
				listener.ConflictOccurred(conflict);
			}
		}

		public void ChangeOccurred(IChangeReport change)
		{
			 foreach (IMergeEventListener listener in _listeners)
			{
				listener.ChangeOccurred(change);
			}
		}

		public void EnteringContext(string context)
		{
			 foreach (IMergeEventListener listener in _listeners)
			{
				listener.EnteringContext(context);
			}
		}
	}

	public class HumanLogMergeEventListener : IMergeEventListener, IDisposable
	{
		private StreamWriter _stream;

		public HumanLogMergeEventListener(string path)
		{
			_stream = File.CreateText(path);
		}
		public void ConflictOccurred(IConflict conflict)
		{
			_stream.WriteLine(conflict.GetFullHumanReadableDescription());
		}

		public void ChangeOccurred(IChangeReport change)
		{
			_stream.WriteLine(change.ToString());
		}

		public void EnteringContext(string context)
		{

		}

		public void Dispose()
		{
			_stream.Close();
		}
	}

	/// <summary>
	/// Note, the conflict log is kept in xml, but that doesn't mean this is only for merging xml documents.
	/// </summary>
	public class XmlLogMergeEventListener : IMergeEventListener, IDisposable
	{
		private XmlWriter _writer;
		private XmlDocument _xmlDoc;
		private string _path;
		static public string TimeFormatNoTimeZone = "yyyy-MM-ddTHH:mm:ssZ";

		/// <summary>
		/// used for finding the context in the orginal file of any conflicts which may occur inside the element
		/// </summary>
		private string _context=string.Empty;

		static public string GetXmlConflictFilePath(string baseXmlFile)
		{
			return baseXmlFile + ".conflicts";
		}

		public XmlLogMergeEventListener(string path)
		{
			_path = path;

			try
			{
				if (!File.Exists(path))
				{
					XmlDocument doc = new XmlDocument();
					doc.LoadXml("<conflicts/>");
					doc.Save(path);
				 }
			}
			catch (Exception error)
			{
				Debug.Fail("Something went wrong trying to create a blank onflict file :"+error.Message);
				//todo log that the xml was the wrong format
			}

			_xmlDoc = new XmlDocument();
			_xmlDoc.Load(path);
			_writer = _xmlDoc.CreateNavigator().SelectSingleNode("conflicts").AppendChild();
		}
		public void ConflictOccurred(IConflict conflict)
		{
			_writer.WriteStartElement("conflict");
			_writer.WriteAttributeString("type", string.Empty, conflict.ConflictTypeHumanName);
			_writer.WriteAttributeString("guid", string.Empty, conflict.Guid.ToString());
			_writer.WriteAttributeString("date", string.Empty, DateTime.UtcNow.ToString(TimeFormatNoTimeZone));
			_writer.WriteAttributeString("context", string.Empty, _context);
			conflict.PathToUnitOfConflict = _context;
			_writer.WriteString(conflict.GetFullHumanReadableDescription());
			_writer.WriteEndElement();
		}

		public void ChangeOccurred(IChangeReport change)
		{
			_writer.WriteStartElement("change");
			_writer.WriteAttributeString("type", string.Empty, change.ActionLabel);
			_writer.WriteAttributeString("guid", string.Empty, change.Guid.ToString());
			_writer.WriteAttributeString("date", string.Empty, DateTime.UtcNow.ToString(TimeFormatNoTimeZone));
			_writer.WriteAttributeString("context", string.Empty, _context);
			//change.PathToUnitOfConflict = _context;
			_writer.WriteString(change.GetFullHumanReadableDescription());
			_writer.WriteEndElement();

		}

		public void EnteringContext(string context)
		{
			_context = context;
		}

		public void Dispose()
		{
			_writer.Close();
			_xmlDoc.Save(_path);
		}
	}


//    public class MergeReport : IMergeEventListener
//    {
//        private List<IConflict> _conflicts=new List<IConflict>();
//        //private string _result;
//        public void ConflictOccurred(IConflict conflict)
//        {
//            _conflicts.Add(conflict);
//        }
//    }

//    public interface IMergeReportMaker
//    {
//        MergeReport GetReport();
//    }

//    public class DefaultMergeReportMaker : IMergeReportMaker
//    {
//
//        public MergeReport GetReport()
//        {
//            return new MergeReport();
//        }
//
//    }
}