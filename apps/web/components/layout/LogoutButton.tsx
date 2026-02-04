"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { logout } from "@/lib/auth";
import { toastApiError } from "@/lib/toast";

export function LogoutButton() {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);

  const handleLogout = async () => {
    setIsLoading(true);
    try {
      await logout();
      router.replace("/login");
      router.refresh();
    } catch (error) {
      toastApiError(error, "Sign out failed. Try again.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Button variant="outline" size="sm" onClick={handleLogout} disabled={isLoading}>
      {isLoading ? "Signing out..." : "Sign out"}
    </Button>
  );
}
