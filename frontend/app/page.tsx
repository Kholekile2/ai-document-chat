import Link from "next/link";

export default function HomePage() {
  return (
    <main className="min-h-screen flex flex-col">
      {/* Navbar */}
      <nav className="border-b border-gray-200 bg-white">
        <div className="max-w-6xl mx-auto px-4 h-16 flex items-center justify-between">
          <span className="text-xl font-bold text-blue-600">DocChat</span>
          <div className="flex items-center gap-4">
            <Link href="/login" className="text-sm text-gray-600 hover:text-gray-900">
              Log in
            </Link>
            <Link
              href="/signup"
              className="text-sm bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
            >
              Get started
            </Link>
          </div>
        </div>
      </nav>

      {/* Hero */}
      <section className="flex-1 flex flex-col items-center justify-center text-center px-4 py-24">
        <h1 className="text-5xl font-bold tracking-tight text-gray-900 max-w-2xl">
          Chat with your documents using AI
        </h1>
        <p className="mt-6 text-xl text-gray-500 max-w-xl">
          Upload any PDF and ask questions in plain English. DocChat finds the
          answers instantly — no more manual searching.
        </p>
        <div className="mt-10 flex gap-4">
          <Link
            href="/signup"
            className="bg-blue-600 text-white px-6 py-3 rounded-lg text-base font-medium hover:bg-blue-700 transition-colors"
          >
            Start for free
          </Link>
          <Link
            href="/login"
            className="border border-gray-300 text-gray-700 px-6 py-3 rounded-lg text-base font-medium hover:bg-gray-50 transition-colors"
          >
            Log in
          </Link>
        </div>
      </section>

      {/* Features */}
      <section className="bg-white border-t border-gray-100 py-20 px-4">
        <div className="max-w-5xl mx-auto grid grid-cols-1 md:grid-cols-3 gap-10">
          <div className="flex flex-col items-center text-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-blue-50 flex items-center justify-center text-2xl">
              📄
            </div>
            <h3 className="font-semibold text-gray-900">Upload any PDF</h3>
            <p className="text-sm text-gray-500">
              Drag and drop your documents. We handle the rest.
            </p>
          </div>
          <div className="flex flex-col items-center text-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-blue-50 flex items-center justify-center text-2xl">
              💬
            </div>
            <h3 className="font-semibold text-gray-900">Ask anything</h3>
            <p className="text-sm text-gray-500">
              Ask questions in natural language and get accurate answers.
            </p>
          </div>
          <div className="flex flex-col items-center text-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-blue-50 flex items-center justify-center text-2xl">
              ⚡
            </div>
            <h3 className="font-semibold text-gray-900">Instant results</h3>
            <p className="text-sm text-gray-500">
              Powered by AI — get streamed answers in seconds.
            </p>
          </div>
        </div>
      </section>

      <footer className="border-t border-gray-100 py-6 text-center text-sm text-gray-400">
        © {new Date().getFullYear()} DocChat. Built for portfolio purposes.
      </footer>
    </main>
  );
}