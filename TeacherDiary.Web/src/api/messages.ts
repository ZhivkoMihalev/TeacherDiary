import { client } from './client'
import type { ConversationDto, MessageDto, MessageContactDto, TranslateRequestDto, TranslateResponseDto } from '../types'

export const messagesApi = {
  getContacts: () =>
    client.get<MessageContactDto[]>('/messages/contacts').then((r) => r.data),

  getUnreadCount: () =>
    client.get<number>('/messages/unread-count').then((r) => r.data),

  getConversations: () =>
    client.get<ConversationDto[]>('/messages/conversations').then((r) => r.data),

  getConversation: (otherUserId: string) =>
    client.get<MessageDto[]>(`/messages/conversations/${otherUserId}`).then((r) => r.data),

  send: (receiverId: string, content: string | null, imageUrl?: string | null) =>
    client.post<{ messageId: string }>('/messages', { receiverId, content, imageUrl }).then((r) => r.data),

  uploadImage: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return client.post<{ imageUrl: string }>('/messages/upload-image', form).then((r) => r.data)
  },
}

export const translationApi = {
  translate: (text: string, targetLanguage: string) =>
    client
      .post<TranslateResponseDto>('/translation/translate', { text, targetLanguage } satisfies TranslateRequestDto)
      .then((r) => r.data),
}
