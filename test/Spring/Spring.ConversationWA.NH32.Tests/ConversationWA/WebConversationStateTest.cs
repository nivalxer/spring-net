using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Spring.ConversationWA;
using Spring.ConversationWA.Imple;
using NUnitAspEx;
using NUnitAspEx.Core;
using Spring.Context.Support;
using NUnitAspEx.Client;
using Spring.Bsn;
using NHibernate;
using System.Text.RegularExpressions;
using Common.Logging;

namespace Spring.ConversationWA
{
    /// <summary>
    /// Test for <see cref="WebConversationStateTest"/>.
    /// </summary>
    [TestFixture]
    public class WebConversationStateTest
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(WebConversationStateTest));

        private IAspFixtureHost host;

        /// <summary>
        /// Test Setup.
        /// </summary>
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            host = AspFixtureHost.CreateInstance("/", "/Data/Spring/ConversationWA/WebConversationStateTest", this);
        }

        /// <summary>
        /// Test TearDown
        /// </summary>
        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            host.Dispose();
        }

        /// <summary>
        /// Test to confirm the end of the conversation after
        /// <see cref="IConversationState.EndConversation()"/>.
        /// </summary>
        [Test]
        public void EndConversationTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(EndConversationTestInHostImpl));
        }

        //"this code is executed entirely within the web application domain"
        private static void EndConversationTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //Start EndConversationTestBegin 
            string content = clnt.GetPage("/EndConversationTestBegin.aspx").Trim();
            Assert.AreEqual("OK", content, "Start EndConversationTestBegin : Request Error!! " + content);
            String uniqIdOrig = (String)AspTestContext.HttpContext.Session["ConversationEvidenceBsn_UniqId"];

            content = clnt.GetPage("/EndConversationTestBegin.aspx").Trim();
            Assert.AreEqual("OK", content, "Start EndConversationTestBegin : Request Error!! " + content);
            Assert.AreEqual(
                uniqIdOrig,
                AspTestContext.HttpContext.Session["ConversationEvidenceBsn_UniqId"]
                );

            //End EndConversationTestEnd 
            content = clnt.GetPage("/EndConversationTestEnd.aspx").Trim();
            Assert.AreEqual("OK", content, "EndConversationTestEnd: Request Error!! " + content);

            //ReStart EndConversationTestBegin
            content = clnt.GetPage("/EndConversationTestBegin.aspx").Trim();
            Assert.AreEqual("OK", content, "ReStart EndConversationTestBegin: Request Error!! " + content);
            Assert.AreNotEqual(
                uniqIdOrig,
                AspTestContext.HttpContext.Session["ConversationEvidenceBsn_UniqId"]
                );
        }

        /// <summary>
        /// Test to confirm the rejection of circular dependency between conversations.
        /// </summary>
        [Test]
        public void CircularDependenceTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(CircularDependenceTestInHostImpl));
        }

        private static void CircularDependenceTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //Start convCircularDependenceTest_A 
            string content = clnt.GetPage("/CircularDependenceTest.aspx").Trim();
            string circularDependenceTestStr = (String)AspTestContext.HttpContext.Session["CircularDependenceTest"];
            Assert.IsTrue(String.IsNullOrEmpty(circularDependenceTestStr), "CircularDependenceTest on the server-side: \n" + circularDependenceTestStr + "\n" + content);
        }

        /// <summary>
        /// Test to see the end of nested conversations 
        /// <see cref="IConversationState.EndConversation()"/>.
        /// </summary>
        [Test]
        public void PatialEndConvTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(PatialEndConvTestInHostImpl));
        }

        private static void PatialEndConvTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();
            String convPatialEndConv_A_BSts =
                "{Id='convPatialEndConv_A_B'; " +
                "this.ParenteConversation.Id=convPatialEndConv_A;" +
                " InnerConversations=[{Id='convPatialEndConv_A_B_A';" +
                " this.ParenteConversation.Id=convPatialEndConv_A_B;" +
                " InnerConversations=[]}{Id='convPatialEndConv_A_B_B';" +
                " this.ParenteConversation.Id=convPatialEndConv_A_B;" +
                " InnerConversations=[]}]}";

            //Start PatialEndConv_A_Begin 
            string content = clnt.GetPage("/PatialEndConv_A_Begin.aspx").Trim();
            Assert.AreEqual("OK", content, "PatialEndConv_A_Begin.aspx : Request Error!! " + content);
            String conversationStr = (String)AspTestContext.HttpContext.Session["ConversationStr"];
            Assert.IsTrue(conversationStr.Contains(convPatialEndConv_A_BSts), String.Format("convPatialEndConv_A_BSts was NOT found in '{0}'", conversationStr));

            //remove PatialEndConv_A_B_End
            content = clnt.GetPage("/PatialEndConv_A_B_End.aspx").Trim();
            Assert.AreEqual("OK", content, "PatialEndConv_A_B_End.aspx : Request Error!! " + content);

            //Start PatialEndConv_A_Begin with 'convPatialEndConv_A_B' ended.
            content = clnt.GetPage("/PatialEndConv_A_Begin.aspx").Trim();
            Assert.AreEqual("OK", content, "PatialEndConv_A_Begin.aspx : Request Error!! " + content);
            conversationStr = (String)AspTestContext.HttpContext.Session["ConversationStr"];
            Assert.IsFalse(conversationStr.Contains(convPatialEndConv_A_BSts), String.Format("convPatialEndConv_A_BSts was FOUND in '{0}'", conversationStr));
        }

        /// <summary>
        /// Test for getting parent conversation scoped value from child conversation. <see cref="IConversationState"/>.
        /// </summary>
        [Test]
        public void GetParentObjetFromChildTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(GetParentObjetFromChildTestInHostImpl));
        }

        private static void GetParentObjetFromChildTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //Start PatialEndConv_A_Begin 
            string content = clnt.GetPage("/GetParentObjetFromChild.aspx").Trim();
            Assert.AreEqual("OK", content, "GetParentObjetFromChild.aspx : Request Error!! " + content);
            Assert.AreEqual("parentValue", AspTestContext.HttpContext.Session["parentKey"]);
            Assert.AreEqual("childValue", AspTestContext.HttpContext.Session["childKey"]);
            Assert.AreEqual("overwrittenValueChild", AspTestContext.HttpContext.Session["overwrittenKey"]);
        }

        /// <summary>
        /// Test for conversation timeout.
        /// </summary>
        [Test]
        public void TimeOutTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(TimeOutTestInHostImpl));
        }

        private static void TimeOutTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //1st TimeOut_NoTimeOut 
            string content = clnt.GetPage("/TimeOut_NoTimeOut.aspx").Trim();
            Assert.AreEqual("OK", content, "1st TimeOut_NoTimeOut.aspx : Request Error!! " + content);
            Assert.AreEqual("this is the orinal value", AspTestContext.HttpContext.Session["keyTimeOut_Old"]);
            Assert.AreEqual("this is the new value", AspTestContext.HttpContext.Session["keyTimeOut_New"]);

            //2nd TimeOut_NoTimeOut 
            content = clnt.GetPage("/TimeOut_NoTimeOut.aspx").Trim();
            Assert.AreEqual("OK", content, "2nd TimeOut_NoTimeOut.aspx : Request Error!! " + content);
            Assert.AreEqual("this is the new value", AspTestContext.HttpContext.Session["keyTimeOut_Old"]);
            Assert.AreEqual("this is the new value", AspTestContext.HttpContext.Session["keyTimeOut_New"]);

            //TimeOut_WithTimeOut 
            content = clnt.GetPage("/TimeOut_WithTimeOut.aspx").Trim();
            Assert.AreEqual("OK", content, "TimeOut_WithTimeOut.aspx : Request Error!! " + content);

            //1th after time out TimeOut_NoTimeOut 
            content = clnt.GetPage("/TimeOut_NoTimeOut.aspx").Trim();
            Assert.AreEqual("OK", content, "1th after time out TimeOut_NoTimeOut.aspx : Request Error!! " + content);
            Assert.AreEqual("this is the orinal value", AspTestContext.HttpContext.Session["keyTimeOut_Old"]);
            Assert.AreEqual("this is the new value", AspTestContext.HttpContext.Session["keyTimeOut_New"]);

            //2th after time out TimeOut_NoTimeOut 
            content = clnt.GetPage("/TimeOut_NoTimeOut.aspx").Trim();
            Assert.AreEqual("OK", content, "2th after time out TimeOut_NoTimeOut.aspx : Request Error!! " + content);
            Assert.AreEqual("this is the new value", AspTestContext.HttpContext.Session["keyTimeOut_Old"]);
            Assert.AreEqual("this is the new value", AspTestContext.HttpContext.Session["keyTimeOut_New"]);
        }

        /// <summary>
        /// Test lazy load for conversation on 'session-per-conversation'.
        /// </summary>
        [Test]
        public void SPCLazyLoadTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(SPCLazyLoadTestInHostImpl));
        }

        private static void SPCLazyLoadTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //NO Raise Lazy
            //Load without details
            string content = clnt.GetPage("/SPCLazyLoadTest_A_Begin.aspx?endConversation=false").Trim();
            Assert.AreEqual("OK", content, "/SPCLazyLoadTest_A_Begin.aspx?endConversation=false (Load without details): Request Error!! " + content);

            //Lazy Load details
            content = clnt.GetPage("/SPCLazyLoadTest_A_Status.aspx").Trim();
            String messageTest = (String)AspTestContext.HttpContext.Session["messageTest"];
            Assert.AreEqual("OK", content, "/SPCLazyLoadTest_A_Status.aspx (Lazy Load details) : Request Error!! " + content);
            Assert.AreEqual("no lazy error", messageTest, "/SPCLazyLoadTest_A_Status.aspx (Lazy Load details) : Request Error!! \n" + messageTest);

            //Raise Lazy
            //End Conversation
            content = clnt.GetPage("/SPCLazyLoadTest_A_Begin.aspx?endConversation=true").Trim();
            Assert.AreEqual("OK", content, "/SPCLazyLoadTest_A_Begin.aspx?endConversation=true (End Conversation) : Request Error!! " + content);

            //Load without details
            content = clnt.GetPage("/SPCLazyLoadTest_A_Begin.aspx?endConversation=false").Trim();
            Assert.AreEqual("OK", content, "/SPCLazyLoadTest_A_Begin.aspx?endConversation=false (Load without details): Request Error!! " + content);

            //ReEnd Conversation
            content = clnt.GetPage("/SPCLazyLoadTest_A_Begin.aspx?endConversation=true").Trim();
            Assert.AreEqual("OK", content, "/SPCLazyLoadTest_A_Begin.aspx?endConversation=true (ReEnd Conversation) : Request Error!! " + content);

            //Raise Lazy Load Error
            content = clnt.GetPage("/SPCLazyLoadTest_A_Status.aspx").Trim();
            messageTest = (String)AspTestContext.HttpContext.Session["messageTest"];
            Assert.AreEqual("OK", content, "/SPCLazyLoadTest_A_Status.aspx (Raise Lazy Load Error): Request Error!! " + content);
            Assert.IsTrue(messageTest.Contains("LazyInitializationException"), "/SPCLazyLoadTest_A_Begin.aspx?endConversation=true (End Conversation) \n" + messageTest);
        }

        /// <summary>
        /// Test switch to one conversation to another at same request(thread).
        /// </summary>
        [Test]
        public void SPCSwitchConversationSameRequestTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(SPCSwitchConversationSameRequestTestInHostImpl));
        }

        private static void SPCSwitchConversationSameRequestTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //NO Raise Lazy
            //Load without details
            string content = clnt.GetPage("/SPCSwitchConversationSameRequest.aspx").Trim();
            Assert.AreEqual("OK", content, "/SPCSwitchConversationSameRequest.aspx (Load without details): Request Error!! " + content);

            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["testeRaizeLazy_A"], (String)AspTestContext.HttpContext.Session["testeRaizeLazy_A"]);
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["testeRaizeLazy_B"], (String)AspTestContext.HttpContext.Session["testeRaizeLazy_B"]);
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["testeNoRaizeLazy_A"], (String)AspTestContext.HttpContext.Session["testeNoRaizeLazy_A"]);
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["testeNoRaizeLazy_B"], (String)AspTestContext.HttpContext.Session["testeNoRaizeLazy_B"]);
        }

        /// <summary>
        /// Test for NOT Raise InvalidOperationException with "Conversation already has another manager".
        /// </summary>
        [Test]
        public void IoeAlreadyHasAnotherManagerNotRaiseTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(IoeAlreadyHasAnotherManagerNotRaiseTestInHostImpl));
        }

        private static void IoeAlreadyHasAnotherManagerNotRaiseTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //NOT Raise InvalidOperationException with "Conversation already has another manager".
            IoeTestsHelper(host, clnt, "reset");
            IoeTestsHelper(host, clnt, "alreadyHasAnotherManagerNotRaise");
        }

        /// <summary>
        ///  Test for Raise InvalidOperationException with ""Conversation already has another manager".
        /// </summary>
        [Test]
        public void IoeAlreadyHasAnotherManagerRaiseTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(IoeAlreadyHasAnotherManagerRaiseTestInHostImpl));
        }

        private static void IoeAlreadyHasAnotherManagerRaiseTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //Test for Raise InvalidOperationException with ""Conversation already has another manager".
            IoeTestsHelper(host, clnt, "reset");
            IoeTestsHelper(host, clnt, "alreadyHasAnotherManagerRaise");
        }

        /// <summary>
        ///  Test for Raise InvalidOperationException with "This conversation already has a different parent".
        /// </summary>
        [Test]
        public void IoeConversationAlreadyDifferentParentTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(IoeConversationAlreadyDifferentParentTestInHostImpl));
        }

        private static void IoeConversationAlreadyDifferentParentTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //Test for Raise InvalidOperationException with "This conversation already has a different parent".
            IoeTestsHelper(host, clnt, "reset");
            IoeTestsHelper(host, clnt, "conversationAlreadyDifferentParent");
        }

        /// <summary>
        ///  Test for Raise InvalidOperationException with "This Conversation is not new." on set <see cref="IConversationState.ParenteConversation"/>.
        /// </summary>
        [Test]
        public void IoeSetParentConversationIsNotNewTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(IoeSetParentConversationIsNotNewTestInHostImpl));
        }

        private static void IoeSetParentConversationIsNotNewTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //Test for Raise InvalidOperationException with "This conversation already has a different parent".
            IoeTestsHelper(host, clnt, "reset");
            IoeTestsHelper(host, clnt, "setParentConversationIsNotNew");
        }

        /// <summary>
        ///  Test for Raise InvalidOperationException with 
        /// "StartResumeConversation: this conversation is ended" on 
        /// <see cref="WebConversationSpringState.StartResumeConversation()"/>.
        /// </summary>
        [Test]
        public void IoeStartResumeConversationIsEndedTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(IoeStartResumeConversationIsEndedTestInHostImpl));
        }

        private static void IoeStartResumeConversationIsEndedTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //Test for Raise InvalidOperationException with "This conversation already has a different parent".
            IoeTestsHelper(host, clnt, "reset");
            IoeTestsHelper(host, clnt, "startResumeConversationIsEnded");
        }

        /// <summary>
        ///  Test for Raise InvalidOperationException with 
        /// "StartResumeConversation: this.SessionFactory.GetCurrentSession() 
        /// have a different instance than 'RootSessionPerConversation'" on 
        /// <see cref="WebConversationSpringState.StartResumeConversation()"/>.
        /// </summary>
        [Test]
        public void IoeParticipatingHibernateNotAlowedTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(IoeParticipatingHibernateNotAlowedTestInHostImpl));
        }

        private static void IoeParticipatingHibernateNotAlowedTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //Test for Raise InvalidOperationException with "This conversation already has a different parent".
            IoeTestsHelper(host, clnt, "reset");
            IoeTestsHelper(host, clnt, "participatingHibernateNotAlowed");
        }

        /// <summary>
        ///  Test for Raise InvalidOperationException with 
        /// "StartResumeConversation: this.SessionFactory.GetCurrentSession() 
        /// have a different instance than 'RootSessionPerConversation'" on 
        /// <see cref="WebConversationSpringState.StartResumeConversation()"/>.
        /// </summary>
        [Test]
        public void IoeIdIsDifferentFromSpringNameTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(IoeIdIsDifferentFromSpringNameTestInHostImpl));
        }

        private static void IoeIdIsDifferentFromSpringNameTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //Test for Raise InvalidOperationException with "This conversation already has a different parent".
            IoeTestsHelper(host, clnt, "reset");
            IoeTestsHelper(host, clnt, "idIsDifferentFromSpringName");
        }

        /// <summary>
        /// Helper for Ioe.*Tests
        /// </summary>
        /// <param name="host"></param>
        /// <param name="clnt"></param>
        /// <param name="test"></param>
        private static void IoeTestsHelper(IAspFixtureHost host, HttpWebClient clnt, String test)
        {
            //Test for Raise Invalid OperationException with ""Conversation already has another manager".
            string content = clnt.GetPage("/IoeTests.aspx?test=" + test).Trim();
            Assert.AreEqual("OK", content, "/IoeTests.aspx?test=" + test + ": Request Error!! " + content);
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["testResult"], (String)AspTestContext.HttpContext.Session["testResult"]);
        }

        /// <summary>
        ///  Test for Raise 'No Hibernate Session bound to thread, and configuration does not allow creation of non-transactional one here'
        /// <see cref="WebConversationSpringState.StartResumeConversation()"/>.
        /// </summary>
        [Test]
        public void RedirectErrorNoPauseConversationTeste()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(RedirectErrorNoPauseConversationTesteInHostImpl));
        }

        private static void RedirectErrorNoPauseConversationTesteInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            string content = "";

            //obtain_session_cookie
            content = clnt.GetPage("/RedirectErrorNoPauseConversation.aspx?step=obtain_session_cookie").Trim();
            Assert.AreEqual("OK", content, "/RedirectErrorNoPauseConversation.aspx?step=obtain_session_cookie\n" + content);

            //step_01
            content = clnt.GetPage("/RedirectErrorNoPauseConversation.aspx?step=step_01").Trim();
            Regex rx = new Regex(@"(?is).*Object moved to.*RedirectErrorNoPauseConversation\.aspx\.*");
            Assert.IsTrue(rx.IsMatch(content), "/RedirectErrorNoPauseConversation.aspx?step=step_02\nNo redirect found\n" + content);

            //step_02
            content = clnt.GetPage("/RedirectErrorNoPauseConversation.aspx?step=step_02").Trim();
            Assert.AreEqual("OK", content, "/RedirectErrorNoPauseConversation.aspx?step=step_02\n" + content);

            content = clnt.GetPage("/RedirectErrorNoPauseConversation.aspx?step=end_conversation").Trim();
            Assert.AreEqual("OK", content, "/RedirectErrorNoPauseConversation.aspx?step=end_conversation\n" + content);

            //obtain_session_cookie
            content = clnt.GetPage("/RedirectErrorNoPauseConversation.aspx?step=obtain_session_cookie").Trim();
            Assert.AreEqual("OK", content, "/RedirectErrorNoPauseConversation.aspx?step=obtain_session_cookie\n" + content);

            //Some_Exception
            content = clnt.GetPage("/RedirectErrorNoPauseConversation.aspx?step=Some_Exception").Trim();
            Assert.IsTrue(content.Contains("Some_Exception"), "/RedirectErrorNoPauseConversation.aspx?step=Some_Exception\n" + content);

            //Post_Some_Exception
            content = clnt.GetPage("/RedirectErrorNoPauseConversation.aspx?step=Post_Some_Exception").Trim();
            Assert.AreEqual("OK", content, "/RedirectErrorNoPauseConversation.aspx?step=Post_Some_Exception\n" + content);

            content = clnt.GetPage("/RedirectErrorNoPauseConversation.aspx?step=end_conversation").Trim();
            Assert.AreEqual("OK", content, "/RedirectErrorNoPauseConversation.aspx?step=end_conversation\n" + content);
        }

        /// <summary>
        /// Test for <see cref="IConversationManager.EndPaused"/>.
        /// </summary>
        [Test]
        public void EndPausedTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(EndPausedTestInHostImpl));
        }

        //"this code is executed entirely within the web application domain"
        private static void EndPausedTestInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //begin
            string content = clnt.GetPage("/EndPausedTest.aspx?testPhase=begin").Trim();
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["result"]);

            //startConvA
            content = clnt.GetPage("/EndPausedTest.aspx?testPhase=startConvA").Trim();
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["result"], content);
        }

        /// <summary>
        /// Test to producing the error "HibernateException: Session is closed..." 
        /// This is because in 
        /// "SessionPerConversationScope.LazySessionPerConversationHolder.CloseConversation(IConversationState)"  
        /// we are closing the "SessionPerConversationScope.LazySessionPerConversationHolder.activeConversation" 
        /// instead of parameter "conversation" (BUG).
        /// </summary>
        [Test]
        public void EndPausedSessionIsClosed()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(EndPausedSessionIsClosedInHostImpl));
        }

        //"this code is executed entirely within the web application domain"
        private static void EndPausedSessionIsClosedInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            string content = clnt.GetPage("/EndPausedSessionIsClosedA.aspx").Trim();
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["result"]);

            //after redirect on EndPausedSessionIsClosedA.aspx
            content = clnt.GetPage("/EndPausedSessionIsClosedB.aspx").Trim();
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["result"]);

            content = clnt.GetPage("/EndPausedSessionIsClosedB.aspx").Trim();
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["result"], content);
        }

        /// <summary>
        /// Test to producing the error "HibernateException: Session is closed..." 
        /// This is because in 
        /// "SessionPerConversationScope.LazySessionPerConversationHolder.CloseConversation(IConversationState)"  
        /// we are closing the "SessionPerConversationScope.LazySessionPerConversationHolder.activeConversation" 
        /// instead of parameter "conversation" (BUG).
        /// </summary>
        [Test]
        public void SessionIsClosed()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(SessionIsClosedInHostImpl));
        }

        //"this code is executed entirely within the web application domain"
        private static void SessionIsClosedInHostImpl()
        {
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            string content = clnt.GetPage("/SessionIsClosedA.aspx").Trim();
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["result"], content);

            content = clnt.GetPage("/SessionIsClosedB.aspx").Trim();
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["result"], content);

            content = clnt.GetPage("/SessionIsClosedB.aspx?command=endA_FreeEnded").Trim();
            Assert.AreEqual("OK", (String)AspTestContext.HttpContext.Session["result"], content);
        }

        /// <summary>
        /// Issue for <see cref="NHibernate.Cfg.Settings.ConnectionReleaseMode"/>
        /// </summary>
        /// <remarks>
        ///   <para>I noticed that outside the "transaction boundaries", each statement
        /// execution (lazy loads) was being made ​​a call to
        /// "Spring.Data.Common.IDbProvider.CreateConnection()". This would
        /// cause a large over-reading because most of the "lazy loads" tend to
        /// occur outside the "transaction boundaries".
        /// The solution to this is to use "connection.release_mode" with "on_close"
        /// in "HibernateProperties".
        ///   </para>
        ///   <para>This test was created to show how the connection openings
        /// occur in the following scenarios:
        ///   <list type="bullet">
        ///   <item>Test with conversation and "connection.release_mode"
        /// "auto"(<see cref="ConnectionReleaseMode.AfterTransaction"/>).</item>
        ///   <item>Test with NO conversation and "connection.release_mode"
        /// "auto"(<see cref="ConnectionReleaseMode.AfterTransaction"/>)
        /// on block within the scope of "transaction boundary".</item>
        ///   <item>Test with NO conversation and "connection.release_mode"
        /// "auto"(<see cref="ConnectionReleaseMode.AfterTransaction"/>).</item>
        ///   <item>Test with conversation and "connection.release_mode"
        /// "on_close"(<see cref="ConnectionReleaseMode.OnClose"/>).</item>
        ///   </list>
        ///   </para>
        /// </remarks>
        [Test]
        public void ConnectionReleaseModeIssue()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(ConnectionReleaseModeIssueInHostImpl));
        }

        private static void ConnectionReleaseModeIssueInHostImpl()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            string content = clnt.GetPage("/ConnectionReleaseModeIssue.aspx").Trim();

            Assert.IsNotNull(AspTestContext.HttpContext, content);
            Assert.IsNotNull(AspTestContext.HttpContext.Session, content);
            Assert.AreEqual(
                "OK",
                (String)AspTestContext.HttpContext.Session["result"],
                (String)AspTestContext.HttpContext.Session["result"] + "\n" + content);
        }

        [Test]
        public void SerializeConversationTest()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            host.Execute(new TestAction(SerializeConversationTestInHostImpl));
        }

        private static void SerializeConversationTestInHostImpl()
        {
            // use static method to avoid passing a reference to ourselves to the host domain
            IAspFixtureHost host = AspTestContext.Host;
            Assert.IsNotNull(host);

            HttpWebClient clnt = host.CreateWebClient();

            //first time (before serialization)
            string content = clnt.GetPage("/SerializeConversationTest.aspx").Trim();

            Assert.IsNotNull(AspTestContext.HttpContext, content);
            Assert.IsNotNull(AspTestContext.HttpContext.Session, content);
            Assert.AreEqual("OK", content, content);

            DateTime beginDt = DateTime.Now;

            //second time (after derialization)
            content = clnt.GetPage("/SerializeConversationTest.aspx").Trim();

            Assert.IsNotNull(AspTestContext.HttpContext, content);
            Assert.IsNotNull(AspTestContext.HttpContext.Session, content);
            Assert.AreEqual("OK", content, content);

            log.Error(DateTime.Now.Subtract(beginDt));
        }
    }
}
