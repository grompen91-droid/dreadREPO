import Nav from "@/components/Nav";
import Hero from "@/components/Hero";
import Features from "@/components/Features";
import Systems from "@/components/Systems";
import Netcode from "@/components/Netcode";
import Install from "@/components/Install";
import Footer from "@/components/Footer";

export default function Home() {
  return (
    <main className="flex flex-col min-h-screen">
      <Nav />
      <Hero />
      <Features />
      <Systems />
      <Netcode />
      <Install />
      <Footer />
    </main>
  );
}
