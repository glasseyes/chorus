﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Chorus.annotations;
using Chorus.sync;
using Chorus.UI.Notes;
using Chorus.UI.Notes.Browser;
using Chorus.Utilities;
using NUnit.Framework;

namespace Chorus.Tests.notes
{
	[TestFixture]
	public class NotesInProjectModelTests
	{

		[SetUp]
		public void Setup()
		{
			TheUser = new ChorusUser("joe");
		}

		[Test]
		public void GetMessages_NoNotesFiles()
		{
			using (var folder = new TempFolder("NotesModelTests"))
			{
				ProjectFolderConfiguration project = new ProjectFolderConfiguration(folder.Path);
				var m = new NotesInProjectViewModel(TheUser, project, new MessageSelectedEvent(), new ConsoleProgress());
				Assert.AreEqual(0, m.GetMessages().Count());
			}
		}

		protected ChorusUser TheUser
		{
			get;
			set;
		}

		[Test]
		public void GetMessages_FilesInSubDirs_GetsThemAll()
		{
			using (var folder = new TempFolder("NotesModelTests"))
			using (var subfolder = new TempFolder(folder, "Sub"))
			using (new TempFile(folder, "one." + AnnotationRepository.FileExtension, "<notes version='0'><annotation><message/></annotation></notes>"))
			using (new TempFile(subfolder, "two." + AnnotationRepository.FileExtension, "<notes  version='0'><annotation><message/></annotation></notes>"))
			{
				ProjectFolderConfiguration project = new ProjectFolderConfiguration(folder.Path);
				var m = new NotesInProjectViewModel(TheUser, project, new MessageSelectedEvent(), new ConsoleProgress());
				Assert.AreEqual(2, m.GetMessages().Count());
			}
		}

		private TempFile CreateNotesFile(TempFolder folder, string contents)
		{
			return new TempFile(folder, "one." + AnnotationRepository.FileExtension, "<notes version='0'>" + contents + "</notes>");
		}

		[Test]
		public void GetMessages_SearchContainsAuthor_FindsMatches()
		{
			using (var folder = new TempFolder("NotesModelTests"))
			{
				string contents = "<annotation><message author='john'></message></annotation>";
				using (CreateNotesFile(folder, contents))
				{
					ProjectFolderConfiguration project = new ProjectFolderConfiguration(folder.Path);
					var m = new NotesInProjectViewModel(TheUser, project, new MessageSelectedEvent(), new ConsoleProgress());
					m.SearchTextChanged("john");
					Assert.AreEqual(1, m.GetMessages().Count());
				}
			}
		}
		[Test]
		public void GetMessages_SearchContainsClass_FindsMatches()
		{
			using (var folder = new TempFolder("NotesModelTests"))
			{
				string contents = @"<annotation class='question'><message author='john'></message></annotation>
				<annotation class='note'><message author='bob'></message></annotation>";
				using (CreateNotesFile(folder, contents))
				{
					ProjectFolderConfiguration project = new ProjectFolderConfiguration(folder.Path);
					var m = new NotesInProjectViewModel(TheUser, project, new MessageSelectedEvent(), new ConsoleProgress());
					 Assert.AreEqual(2, m.GetMessages().Count(), "should get 2 annotations when search box is empty");
				   m.SearchTextChanged("ques");
					Assert.AreEqual(1, m.GetMessages().Count());
					Assert.AreEqual("john",m.GetMessages().First().Message.Author);

				}
			}
		}
	}

}

