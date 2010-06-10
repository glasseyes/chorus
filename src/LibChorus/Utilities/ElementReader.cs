﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Chorus.Utilities
{
	/// <summary>
	/// Responsible to read a file which has a sequence of similar elements
	/// and pass a byte array of each element to a delgate for processing.
	/// Use low-level byte array methods (eventually asynchronously, I [JohnT] hope).
	/// </summary>
	public class ElementReader : IDisposable
	{
		private byte[] m_inputData;
		private readonly byte[] m_openingMarker;
		private readonly byte[] m_finalClosingTag;
		private Action<byte[]> m_outputHandler;
		private int m_startOfRecordsOffset;
		private int m_endOfRecordsOffset;
		private readonly List<Byte> m_endingWhitespace;

		public ElementReader(string openingMarker, string finalClosingTag, byte[] inputData, Action<byte[]> outputHandler)
		{
			m_inputData = inputData;
			var enc = Encoding.UTF8;
			m_openingMarker = enc.GetBytes(openingMarker);
			m_finalClosingTag = enc.GetBytes(finalClosingTag);
			m_outputHandler = outputHandler;
			m_startOfRecordsOffset = 0;
			m_endOfRecordsOffset = m_inputData.Length;
			m_endingWhitespace = new List<byte>();
			m_endingWhitespace.AddRange(enc.GetBytes(" "));
			m_endingWhitespace.AddRange(enc.GetBytes("\t"));
			m_endingWhitespace.AddRange(enc.GetBytes("\r"));
			m_endingWhitespace.AddRange(enc.GetBytes("\n"));
		}

		public void Run()
		{
			TrimInput();

			if (m_startOfRecordsOffset == m_endOfRecordsOffset)
				return; // Nothing to do.

			var openingAngleBracket = m_openingMarker[0];
			for (var i = m_startOfRecordsOffset; i < m_endOfRecordsOffset; ++i)
			{
				var endOffset = FindStartOfElement(i + 1, openingAngleBracket);
				// We should have the complete <foo> element in the param.
				m_outputHandler(m_inputData.SubArray(i, endOffset - i));
				i = endOffset - 1;
			}
		}

		/// <summary>
		/// This method adjusts m_startOfRecordsOffset to the offset to the start of the records,
		/// and adjusts m_endOfRecordsOffset to the end of the last record.
		/// </summary>
		private void TrimInput()
		{
			// Trim off junk at the start.
			m_startOfRecordsOffset = FindStartOfElement(0, m_openingMarker[0]);
			// Trim off end tag. It really better be the last bunch of bytes!
			m_endOfRecordsOffset = m_inputData.Length - m_finalClosingTag.Length;
		}

		private int FindStartOfElement(int currentOffset, byte openingAngleBracket)
		{
			// Need to get the next starting marker, or the main closing tag
			// When the end point is found, call m_outputHandler with the current array
			// from 'offset' to 'i' (more or less).
			// Skip quickly over anything that doesn't match even one character.
			for (var i = currentOffset; i < m_endOfRecordsOffset; ++i)
			{
				var currentByte = m_inputData[i];
				// Need to get the next starting marker, or the main closing tag
				// When the end point is found, call m_outputHandler with the current array
				// from 'offset' to 'i' (more or less).
				// Skip quickly over anything that doesn't match even one character.
				if (currentByte != openingAngleBracket)
					continue;

				// Try to match the rest of the marker.
				for (var j = 1; ; j++)
				{
					var current = m_inputData[i + j];
					if (m_endingWhitespace.Contains(current))
					{
						// Got it!
						return i;
					}
					if (m_openingMarker[j] != current)
						break; // no match, resume searching for opening character.
					if (j != m_openingMarker.Length - 1)
						continue;
				}
			}

			return m_endOfRecordsOffset; // Found the end.
		}

		~ElementReader()
		{
			Debug.WriteLine("**** ElementReader.Finalizer called ****");
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private bool m_isDisposed;
		private void Dispose(bool disposing)
		{
			if (m_isDisposed)
				return; // Done already, so nothing left to do.

			if (disposing)
			{
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			// Main data members.
			m_inputData = null;
			m_outputHandler = null;

			m_isDisposed = true;
		}
	}
}