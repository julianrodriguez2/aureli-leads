import { cookies } from "next/headers";
import { UserManagementClient } from "@/components/users/UserManagementClient";
import { AUTH_COOKIE_NAME, listUsers, me } from "@/lib/auth";
import type { MeDto, UserDto } from "@/lib/types";

export default async function UsersPage() {
  const token = cookies().get(AUTH_COOKIE_NAME)?.value;
  let currentUser: MeDto | null = null;
  let users: UserDto[] = [];

  if (token) {
    try {
      currentUser = await me(`${AUTH_COOKIE_NAME}=${token}`);
    } catch {
      currentUser = null;
    }

    if (currentUser?.role?.toLowerCase() === "admin") {
      try {
        users = await listUsers(`${AUTH_COOKIE_NAME}=${token}`);
      } catch {
        users = [];
      }
    }
  }

  const isAdmin = currentUser?.role?.toLowerCase() === "admin";

  return (
    <UserManagementClient initialUsers={users} isAdmin={isAdmin} />
  );
}
