# DocChat — AI-Powered Document Chat

A full-stack AI application that lets you upload PDF documents and have intelligent conversations about their content using Retrieval-Augmented Generation (RAG).

**Live Demo:** [ai-document-chat-sigma.vercel.app](https://ai-document-chat-sigma.vercel.app)

---

## What It Does

Upload any PDF and ask questions about it in natural language. DocChat finds the most relevant sections of your document and uses GPT-4o-mini to generate accurate, grounded answers — streamed to you in real time, word by word.

- Ask "What are the key findings?" and get a precise answer from the document
- Ask follow-up questions in the same conversation
- Come back later and your conversation history is still there
- Upload multiple documents and chat with each one separately

---

## Tech Stack

### Frontend
- **Next.js 14** (App Router, Server Components, Server Actions)
- **TypeScript**
- **Tailwind CSS**
- **Supabase Auth** (email/password authentication)

### Backend
- **ASP.NET Core** (.NET 8 Minimal APIs)
- **PdfPig** (PDF text extraction)
- **OpenAI API** (text-embedding-ada-002 + GPT-4o-mini)

### Database & Infrastructure
- **Supabase** (PostgreSQL + pgvector extension)
- **pgvector** (vector similarity search)
- **Railway** (backend hosting)
- **Vercel** (frontend hosting)

---

## How It Works — The RAG Pipeline

RAG (Retrieval-Augmented Generation) is the AI pattern that makes DocChat accurate and grounded in your documents rather than making things up.

```
PDF Upload
    ↓
Text Extraction (PdfPig)
    ↓
Chunking — split into ~500 word overlapping chunks
    ↓
Embedding — OpenAI converts each chunk to a 1536-dimension vector
    ↓
Storage — vectors saved to Supabase pgvector

--- When you ask a question ---

Question
    ↓
Embed the question — same OpenAI model
    ↓
Vector similarity search — find the 5 most relevant chunks
    ↓
Build prompt — inject chunks as context for the LLM
    ↓
Stream response — GPT-4o-mini answers using only the document content
    ↓
Save to conversation history
```

---

## Features

- **Authentication** — sign up, log in, protected routes, session persistence
- **PDF Upload** — upload any PDF with real-time processing progress
- **Semantic Search** — finds relevant content even when your question uses different words than the document
- **Streaming Chat** — responses appear word by word in real time
- **Conversation History** — all conversations persist and reload automatically
- **Multiple Documents** — upload and chat with multiple PDFs independently
- **Delete Documents** — two-step confirmation delete with automatic cleanup of all related data

---

## Project Structure

```
ai-document-chat/
├── frontend/                          # Next.js 14 application
│   └── app/
│       ├── actions/auth.ts            # Server actions for authentication
│       ├── chat/[documentId]/         # Dynamic chat page per document
│       ├── components/
│       │   ├── auth/AuthForm.tsx      # Login/signup form
│       │   ├── chat/ChatWindow.tsx    # Streaming chat UI with sidebar
│       │   └── documents/            # Upload, list, dashboard components
│       ├── dashboard/                 # Protected dashboard page
│       ├── lib/
│       │   ├── api.ts                 # All backend API calls
│       │   └── supabase/             # Supabase client configuration
│       └── types/document.ts          # Shared TypeScript interfaces
│
└── AiDocChat.Api/                     # ASP.NET Core backend
    ├── Endpoints/
    │   ├── DocumentEndpoints.cs       # Upload, list, delete routes
    │   └── ChatEndpoints.cs           # Chat + conversation routes
    └── Services/
        ├── DocumentService.cs         # Document metadata (Supabase REST)
        ├── PdfService.cs              # Text extraction + chunking
        ├── EmbeddingService.cs        # OpenAI embedding API
        ├── ChunkService.cs            # Vector storage via Supabase RPC
        ├── SearchService.cs           # pgvector similarity search
        ├── ChatService.cs             # GPT streaming via IAsyncEnumerable
        └── ConversationService.cs     # Conversation persistence
```

---

## Database Schema

```sql
documents          — PDF metadata, status, user ownership
document_chunks    — text chunks with 1536-dim vector embeddings
conversations      — chat sessions per user per document
messages           — individual messages in each conversation
```

All tables use Supabase pgvector with cosine similarity search via a custom `match_document_chunks` PostgreSQL function.

---

## Running Locally

### Prerequisites
- Node.js 18+
- .NET 8 SDK
- Supabase account (free tier)
- OpenAI API account

### Backend setup

1. Clone the repo
2. Create `AiDocChat.Api/appsettings.Development.json`:

```json
{
  "Supabase": {
    "Url": "your-supabase-url",
    "AnonKey": "your-anon-key",
    "ServiceKey": "your-service-role-key"
  },
  "OpenAI": {
    "ApiKey": "your-openai-key"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5014" }
    }
  }
}
```

3. Run the backend:
```bash
cd AiDocChat.Api
dotnet run
```

### Frontend setup

1. Create `frontend/.env.local`:
```
NEXT_PUBLIC_SUPABASE_URL=your-supabase-url
NEXT_PUBLIC_SUPABASE_ANON_KEY=your-anon-key
NEXT_PUBLIC_API_URL=http://localhost:5014
```

2. Run the frontend:
```bash
cd frontend
npm install
npm run dev
```

### Supabase setup

Run the following SQL in your Supabase SQL editor to create the required tables and functions — see the full schema in the project documentation.

---

## Deployment

- **Backend** — deployed on [Railway](https://railway.app) with environment variables configured in the Railway dashboard
- **Frontend** — deployed on [Vercel](https://vercel.com) with environment variables configured in the Vercel dashboard
- Auto-deploys on every push to `main`

---

## Key Technical Decisions

**Supabase REST API over direct PostgreSQL** — residential and corporate networks often block port 5432. Using Supabase's REST API over HTTPS (port 443) ensures the app works on any network.

**Per-request HttpClient headers** — ASP.NET Core's injected HttpClient is shared across requests. Setting headers on DefaultRequestHeaders causes race conditions. All services use HttpRequestMessage with per-request headers.

**IAsyncEnumerable for streaming** — C#'s async enumerable pattern maps perfectly to OpenAI's Server-Sent Events stream, allowing token-by-token forwarding from OpenAI to the browser with minimal latency.

**Service role key on backend** — the backend uses Supabase's service role key which bypasses RLS, while the frontend uses only the anon key for auth. Security is enforced at the API layer via user_id filtering.

---

## License

MIT
