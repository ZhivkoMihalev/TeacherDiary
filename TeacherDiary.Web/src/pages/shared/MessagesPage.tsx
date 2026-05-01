import { useState, useEffect, useRef, useCallback } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { messagesApi } from '../../api/messages'
import type { ConversationDto, MessageContactDto } from '../../types'

function formatTime(iso: string) {
  const d = new Date(iso)
  const now = new Date()
  const isToday = d.toDateString() === now.toDateString()
  if (isToday) return d.toLocaleTimeString('bg-BG', { hour: '2-digit', minute: '2-digit' })
  return d.toLocaleDateString('bg-BG', { day: '2-digit', month: '2-digit' })
}

function Avatar({ name, size = 'md' }: { name: string; size?: 'sm' | 'md' }) {
  const initials = name
    .split(' ')
    .map((w) => w[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()
  const dim = size === 'sm' ? 'w-8 h-8 text-xs' : 'w-10 h-10 text-sm'
  return (
    <div className={`${dim} rounded-full bg-indigo-100 text-indigo-700 font-semibold flex items-center justify-center shrink-0`}>
      {initials}
    </div>
  )
}

function NewConversationModal({
  contacts,
  onSelect,
  onClose,
}: {
  contacts: MessageContactDto[]
  onSelect: (contact: MessageContactDto) => void
  onClose: () => void
}) {
  const [search, setSearch] = useState('')
  const trimmed = search.trim()
  const filtered = trimmed
    ? contacts.filter((c) => c.fullName.toLowerCase().includes(trimmed.toLowerCase()))
    : []

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div className="bg-white rounded-xl shadow-xl w-80 max-h-[70vh] flex flex-col">
        <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100">
          <h3 className="font-semibold text-gray-800 text-sm">Ново съобщение</h3>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-lg leading-none">×</button>
        </div>
        <div className="px-4 py-3">
          <input
            autoFocus
            type="text"
            placeholder="Търси..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full text-sm rounded-lg border border-gray-200 px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-indigo-400"
          />
        </div>
        {trimmed && (
          <div className="overflow-y-auto border-t border-gray-100">
            {filtered.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-6">Няма намерени контакти</p>
            ) : (
              filtered.map((c) => (
                <button
                  key={c.userId}
                  onClick={() => onSelect(c)}
                  className="w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors text-left"
                >
                  <Avatar name={c.fullName} size="sm" />
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-gray-800 truncate">{c.fullName}</p>
                    {c.studentName && (
                      <p className="text-xs text-gray-400 truncate">ученик: {c.studentName}</p>
                    )}
                  </div>
                </button>
              ))
            )}
          </div>
        )}
      </div>
    </div>
  )
}

export function MessagesPage() {
  const qc = useQueryClient()
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null)
  const [selectedUserName, setSelectedUserName] = useState('')
  const [input, setInput] = useState('')
  const [showNewModal, setShowNewModal] = useState(false)
  const [isDragging, setIsDragging] = useState(false)
  const [pendingImage, setPendingImage] = useState<File | null>(null)
  const [pendingImagePreview, setPendingImagePreview] = useState<string | null>(null)
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null)
  const bottomRef = useRef<HTMLDivElement>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const dragCounter = useRef(0)

  const { data: conversations = [] } = useQuery({
    queryKey: ['conversations'],
    queryFn: messagesApi.getConversations,
    refetchInterval: 5_000,
  })

  const { data: contactsList = [] } = useQuery({
    queryKey: ['message-contacts'],
    queryFn: messagesApi.getContacts,
  })

  const { data: messages = [] } = useQuery({
    queryKey: ['conversation', selectedUserId],
    queryFn: () => messagesApi.getConversation(selectedUserId!),
    enabled: !!selectedUserId,
    refetchInterval: 3_000,
  })

  const sendMutation = useMutation({
    mutationFn: async () => {
      let imageUrl: string | null = null
      if (pendingImage) {
        const res = await messagesApi.uploadImage(pendingImage)
        imageUrl = res.imageUrl
      }
      const content = input.trim() || null
      return messagesApi.send(selectedUserId!, content, imageUrl)
    },
    onSuccess: () => {
      setInput('')
      clearPendingImage()
      qc.invalidateQueries({ queryKey: ['conversation', selectedUserId] })
      qc.invalidateQueries({ queryKey: ['conversations'] })
      qc.invalidateQueries({ queryKey: ['unread-count'] })
    },
  })

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  function clearPendingImage() {
    if (pendingImagePreview) URL.revokeObjectURL(pendingImagePreview)
    setPendingImage(null)
    setPendingImagePreview(null)
  }

  function attachFile(file: File) {
    if (!file.type.startsWith('image/')) return
    clearPendingImage()
    setPendingImage(file)
    setPendingImagePreview(URL.createObjectURL(file))
  }

  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    dragCounter.current++
    if (e.dataTransfer.types.includes('Files')) setIsDragging(true)
  }, [])

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    dragCounter.current--
    if (dragCounter.current === 0) setIsDragging(false)
  }, [])

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
  }, [])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    dragCounter.current = 0
    setIsDragging(false)
    const file = e.dataTransfer.files[0]
    if (file) attachFile(file)
  }, [])

  function selectConversation(conv: ConversationDto) {
    setSelectedUserId(conv.otherUserId)
    setSelectedUserName(conv.otherUserName)
    qc.invalidateQueries({ queryKey: ['conversations'] })
    qc.invalidateQueries({ queryKey: ['unread-count'] })
  }

  function selectContact(contact: MessageContactDto) {
    setShowNewModal(false)
    setSelectedUserId(contact.userId)
    setSelectedUserName(contact.fullName)
  }

  function handleSend() {
    const hasContent = input.trim() || pendingImage
    if (!hasContent || !selectedUserId || sendMutation.isPending) return
    sendMutation.mutate()
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (file) attachFile(file)
    e.target.value = ''
  }

  const canSend = (input.trim() || pendingImage) && !!selectedUserId && !sendMutation.isPending

  return (
    <div className="flex h-full">
      {/* Sidebar: conversation list */}
      <aside className="w-72 border-r border-gray-200 flex flex-col bg-white shrink-0">
        <div className="px-4 py-3 border-b border-gray-100 flex items-center justify-between">
          <h2 className="font-semibold text-gray-800">Съобщения</h2>
          <button
            onClick={() => setShowNewModal(true)}
            className="text-xs bg-indigo-600 text-white px-3 py-1.5 rounded-lg hover:bg-indigo-700 transition-colors"
          >
            + Ново
          </button>
        </div>

        <div className="flex-1 overflow-y-auto divide-y divide-gray-50">
          {conversations.length === 0 && (
            <p className="text-sm text-gray-400 text-center py-12 px-4">
              Нямате съобщения. Натиснете "Ново", за да започнете разговор.
            </p>
          )}
          {conversations.map((conv) => (
            <button
              key={conv.otherUserId}
              onClick={() => selectConversation(conv)}
              className={`w-full flex items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-gray-50 ${
                selectedUserId === conv.otherUserId ? 'bg-indigo-50' : ''
              }`}
            >
              <Avatar name={conv.otherUserName} />
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between mb-0.5">
                  <p className="text-sm font-medium text-gray-800 truncate">{conv.otherUserName}</p>
                  <span className="text-xs text-gray-400 shrink-0 ml-1">{formatTime(conv.lastMessageAt)}</span>
                </div>
                {conv.studentName && (
                  <p className="text-xs text-indigo-500 truncate mb-0.5">ученик: {conv.studentName}</p>
                )}
                <div className="flex items-center justify-between">
                  <p className="text-xs text-gray-500 truncate">
                    {conv.lastMessageIsFromMe ? 'Вие: ' : ''}
                    {conv.lastMessage}
                  </p>
                  {conv.unreadCount > 0 && (
                    <span className="ml-2 shrink-0 bg-red-500 text-white text-xs font-bold rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-1">
                      {conv.unreadCount}
                    </span>
                  )}
                </div>
              </div>
            </button>
          ))}
        </div>
      </aside>

      {/* Main: message thread */}
      <div className="flex-1 flex flex-col min-w-0 bg-gray-50">
        {!selectedUserId ? (
          <div className="flex-1 flex items-center justify-center text-gray-400 text-sm">
            Изберете разговор или започнете нов
          </div>
        ) : (
          <>
            {/* Thread header */}
            <div className="bg-white border-b border-gray-200 px-6 py-3 flex items-center gap-3">
              <Avatar name={selectedUserName} />
              <div>
                <p className="font-semibold text-gray-800 text-sm">{selectedUserName}</p>
              </div>
            </div>

            {/* Messages — drag-drop zone */}
            <div
              className="flex-1 overflow-y-auto px-6 py-4 space-y-2 relative"
              onDragEnter={handleDragEnter}
              onDragLeave={handleDragLeave}
              onDragOver={handleDragOver}
              onDrop={handleDrop}
            >
              {/* Drag overlay */}
              {isDragging && (
                <div className="absolute inset-0 z-20 flex items-center justify-center bg-indigo-50/90 border-2 border-dashed border-indigo-400 rounded-lg pointer-events-none">
                  <div className="text-center">
                    <div className="text-4xl mb-2">🖼️</div>
                    <p className="text-indigo-600 font-medium text-sm">Пуснете снимката тук</p>
                  </div>
                </div>
              )}

              {messages.length === 0 && (
                <p className="text-center text-gray-400 text-sm mt-12">
                  Нямате разговори с {selectedUserName}. Изпратете първото съобщение.
                </p>
              )}

              {messages.map((msg) => (
                <div
                  key={msg.id}
                  className={`flex ${msg.isFromMe ? 'justify-end' : 'justify-start'}`}
                >
                  <div
                    className={`max-w-[70%] px-4 py-2.5 rounded-2xl text-sm leading-relaxed ${
                      msg.isFromMe
                        ? 'bg-indigo-600 text-white rounded-br-sm'
                        : 'bg-white text-gray-800 shadow-sm rounded-bl-sm'
                    }`}
                  >
                    {msg.imageUrl && (
                      <img
                        src={msg.imageUrl}
                        alt="Снимка"
                        className="rounded-xl max-w-full max-h-64 object-contain cursor-pointer mb-1"
                        onClick={() => setLightboxUrl(msg.imageUrl!)}
                      />
                    )}
                    {msg.content && (
                      <p className="whitespace-pre-wrap break-words">{msg.content}</p>
                    )}
                    <p className={`text-[10px] mt-1 ${msg.isFromMe ? 'text-indigo-200 text-right' : 'text-gray-400'}`}>
                      {formatTime(msg.sentAt)}
                      {msg.isFromMe && (msg.isRead ? ' ✓✓' : ' ✓')}
                    </p>
                  </div>
                </div>
              ))}
              <div ref={bottomRef} />
            </div>

            {/* Input */}
            <div className="bg-white border-t border-gray-200 px-4 py-3 space-y-2">
              {/* Image preview */}
              {pendingImagePreview && (
                <div className="flex items-start gap-2">
                  <div className="relative">
                    <img
                      src={pendingImagePreview}
                      alt="Преглед"
                      className="h-20 rounded-lg object-cover border border-gray-200"
                    />
                    <button
                      onClick={clearPendingImage}
                      className="absolute -top-1.5 -right-1.5 bg-gray-700 text-white rounded-full w-5 h-5 flex items-center justify-center text-xs hover:bg-red-500 transition-colors"
                    >
                      ×
                    </button>
                  </div>
                </div>
              )}

              <div className="flex items-end gap-2">
                {/* Image attach button */}
                <button
                  onClick={() => fileInputRef.current?.click()}
                  title="Прикачи снимка"
                  className="shrink-0 p-2 rounded-lg text-gray-400 hover:text-indigo-600 hover:bg-indigo-50 transition-colors"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" className="w-5 h-5">
                    <path fillRule="evenodd" d="M1 5.25A2.25 2.25 0 013.25 3h13.5A2.25 2.25 0 0119 5.25v9.5A2.25 2.25 0 0116.75 17H3.25A2.25 2.25 0 011 14.75v-9.5zm1.5 5.81v3.69c0 .414.336.75.75.75h13.5a.75.75 0 00.75-.75v-2.69l-2.22-2.219a.75.75 0 00-1.06 0l-1.91 1.909-.48-.48a.75.75 0 00-1.06 0L6.53 11.06l-4.03-4.03v4.03zM5.5 7a1.5 1.5 0 110 3 1.5 1.5 0 010-3z" clipRule="evenodd" />
                  </svg>
                </button>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={handleFileChange}
                />

                <textarea
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder={pendingImage ? 'Добавете подпис (по желание)...' : 'Напишете съобщение...'}
                  rows={1}
                  className="flex-1 resize-none rounded-xl border border-gray-300 px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 max-h-32 overflow-y-auto"
                  style={{ height: 'auto' }}
                  onInput={(e) => {
                    const t = e.currentTarget
                    t.style.height = 'auto'
                    t.style.height = `${Math.min(t.scrollHeight, 128)}px`
                  }}
                />
                <button
                  onClick={handleSend}
                  disabled={!canSend}
                  className="bg-indigo-600 text-white rounded-xl px-4 py-2.5 text-sm font-medium hover:bg-indigo-700 disabled:opacity-40 disabled:cursor-not-allowed transition-colors shrink-0"
                >
                  {sendMutation.isPending ? '...' : 'Изпрати'}
                </button>
              </div>
            </div>
          </>
        )}
      </div>

      {showNewModal && (
        <NewConversationModal
          contacts={contactsList}
          onSelect={selectContact}
          onClose={() => setShowNewModal(false)}
        />
      )}

      {/* Lightbox */}
      {lightboxUrl && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80"
          onClick={() => setLightboxUrl(null)}
        >
          <img
            src={lightboxUrl}
            alt="Снимка"
            className="max-w-[90vw] max-h-[90vh] rounded-xl object-contain"
            onClick={(e) => e.stopPropagation()}
          />
          <button
            onClick={() => setLightboxUrl(null)}
            className="absolute top-4 right-4 text-white text-3xl leading-none hover:text-gray-300"
          >
            ×
          </button>
        </div>
      )}
    </div>
  )
}
