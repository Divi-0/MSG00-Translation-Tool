using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using MSG00.Translation.Infrastructure.Domain.Conversation;
using MSG00.Translation.Infrastructure.Domain.Conversation.Enums;
using System.Collections.Generic;

namespace MSG00.Translation.UI.Controls
{
    internal class ConversationPointerTextDataTemplateSelector : IDataTemplate
    {
        [Content]
        public Dictionary<string, IDataTemplate> Templates { get; } = new Dictionary<string, IDataTemplate>();

        public Control Build(object param)
        {
            ConversationPointerText conversationPointerText = (ConversationPointerText)param;

            if (conversationPointerText.PointerItemType == ConversationPointerTextType.TextWithChangingCharImage)
            {
                return Templates[ConversationPointerTextType.Normal.ToString()].Build(param)!;
            }

            return Templates[conversationPointerText.PointerItemType.ToString()].Build(param)!;
        }

        // Check if we can accept the provided data
        public bool Match(object data)
        {
            if (data == null || data is not ConversationPointerText)
            {
                return false;
            }

            return true;
        }
    }
}
