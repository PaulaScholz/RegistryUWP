//***********************************************************************
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//**********************************************************************​

namespace RegistryUWP
{
    /// <summary>
    /// The XAML will display a green background for a Status message, and a red background for an ErrorMessage,
    /// blue/violet for a warning message.  ClearMessage type clears the Status box and sets it to green.
    /// </summary>
    public enum NotifyType
    {
        StatusMessage,
        WarningMessage,
        ErrorMessage,
        ClearMessage
    };

    /// <summary>
    /// For the Prism EventAggregator, MVVMLight Messenger, PubSub or other messaging system.
    /// </summary>
    public class NotifyUserEventMessage
    {
        public string MessagePayload { get; set; }
        public NotifyType MessageType { get; set; }
    }

}
