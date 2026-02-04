"use client";

import { useMemo, useState } from "react";
import { ApiError } from "@/lib/api";
import { createUser, resetUserPassword, updateUserRole } from "@/lib/auth";
import type { CreateUserRequest, UserDto } from "@/lib/types";
import { toastApiError, toastError, toastSuccess } from "@/lib/toast";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

type UserManagementClientProps = {
  initialUsers: UserDto[];
  isAdmin: boolean;
};

const roleOptions = ["Admin", "Agent", "ReadOnly"] as const;

function formatDate(value?: string | null) {
  if (!value) {
    return "N/A";
  }

  return new Date(value).toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric"
  });
}

function isValidEmail(email: string) {
  return /.+@.+\..+/.test(email);
}

export function UserManagementClient({ initialUsers, isAdmin }: UserManagementClientProps) {
  const [users, setUsers] = useState<UserDto[]>(initialUsers);
  const [roleEdits, setRoleEdits] = useState<Record<string, string>>({});
  const [isCreating, setIsCreating] = useState(false);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [createForm, setCreateForm] = useState<CreateUserRequest>({
    email: "",
    password: "",
    role: "Agent"
  });

  const [resetUser, setResetUser] = useState<UserDto | null>(null);
  const [resetPassword, setResetPassword] = useState("");
  const [isResetting, setIsResetting] = useState(false);

  const roleState = useMemo(() => {
    const map: Record<string, string> = {};
    users.forEach((user) => {
      map[user.id] = roleEdits[user.id] ?? user.role;
    });
    return map;
  }, [roleEdits, users]);

  async function handleCreateUser() {
    if (!isAdmin) {
      toastError("Admin access required.");
      return;
    }

    const trimmedEmail = createForm.email.trim();
    if (!isValidEmail(trimmedEmail)) {
      toastError("Enter a valid email.");
      return;
    }

    if (createForm.password.trim().length < 8) {
      toastError("Password must be at least 8 characters.");
      return;
    }

    setIsCreating(true);
    try {
      const created = await createUser({
        email: trimmedEmail,
        password: createForm.password,
        role: createForm.role
      });
      setUsers((prev) => [...prev, created]);
      setCreateForm({ email: "", password: "", role: "Agent" });
      setIsCreateOpen(false);
      toastSuccess("User created.");
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        toastError("Email already exists.", error);
      } else {
        toastApiError(error, "Unable to create user.");
      }
    } finally {
      setIsCreating(false);
    }
  }

  async function handleRoleSave(userId: string) {
    if (!isAdmin) {
      toastError("Admin access required.");
      return;
    }

    const role = roleState[userId];
    if (!role) {
      return;
    }

    try {
      const updated = await updateUserRole(userId, { role: role as CreateUserRequest["role"] });
      setUsers((prev) => prev.map((user) => (user.id === userId ? updated : user)));
      setRoleEdits((prev) => {
        const next = { ...prev };
        delete next[userId];
        return next;
      });
      toastSuccess("Role updated.");
    } catch (error) {
      toastApiError(error, "Unable to update role.");
    }
  }

  async function handleResetPassword() {
    if (!resetUser) {
      return;
    }

    if (resetPassword.trim().length < 8) {
      toastError("Password must be at least 8 characters.");
      return;
    }

    setIsResetting(true);
    try {
      await resetUserPassword(resetUser.id, { password: resetPassword });
      toastSuccess("Password reset.");
      setResetPassword("");
      setResetUser(null);
    } catch (error) {
      toastApiError(error, "Unable to reset password.");
    } finally {
      setIsResetting(false);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs uppercase tracking-[0.2em] text-muted-foreground">Admin</p>
          <h2 className="text-2xl font-semibold">Users</h2>
        </div>
        {isAdmin ? (
          <Button onClick={() => setIsCreateOpen(true)}>Create user</Button>
        ) : null}
      </div>

      {!isAdmin ? (
        <Card>
          <CardContent className="p-6 text-sm text-muted-foreground">
            Not authorized to manage users.
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>User directory</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Email</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={4} className="py-10 text-center text-sm text-muted-foreground">
                      No users yet.
                    </TableCell>
                  </TableRow>
                ) : (
                  users.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell className="font-medium">{user.email}</TableCell>
                      <TableCell>
                        <select
                          className="h-9 rounded-md border border-border/70 bg-white/80 px-2 text-sm"
                          value={roleState[user.id]}
                          onChange={(event) =>
                            setRoleEdits((prev) => ({
                              ...prev,
                              [user.id]: event.target.value
                            }))
                          }
                        >
                          {roleOptions.map((role) => (
                            <option key={role} value={role}>{role}</option>
                          ))}
                        </select>
                      </TableCell>
                      <TableCell>{formatDate(user.createdAt)}</TableCell>
                      <TableCell className="text-right">
                        <div className="flex flex-wrap items-center justify-end gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handleRoleSave(user.id)}
                            disabled={roleEdits[user.id] === undefined}
                          >
                            Save role
                          </Button>
                          <Button
                            variant="secondary"
                            size="sm"
                            onClick={() => {
                              setResetUser(user);
                              setResetPassword("");
                            }}
                          >
                            Reset password
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {isCreateOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          role="dialog"
          aria-modal="true"
          onClick={() => {
            if (!isCreating) {
              setIsCreateOpen(false);
            }
          }}
        >
          <div
            className="w-full max-w-md rounded-2xl border border-border bg-background p-6 shadow-xl"
            onClick={(event) => event.stopPropagation()}
          >
            <h3 className="text-lg font-semibold">Create user</h3>
            <div className="mt-4 space-y-3">
              <div className="space-y-1">
                <Label htmlFor="create-email">Email</Label>
                <Input
                  id="create-email"
                  type="email"
                  value={createForm.email}
                  onChange={(event) => setCreateForm((prev) => ({ ...prev, email: event.target.value }))}
                  placeholder="user@example.com"
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="create-password">Password</Label>
                <Input
                  id="create-password"
                  type="password"
                  value={createForm.password}
                  onChange={(event) => setCreateForm((prev) => ({ ...prev, password: event.target.value }))}
                  placeholder="At least 8 characters"
                />
              </div>
              <div className="space-y-1">
                <Label htmlFor="create-role">Role</Label>
                <select
                  id="create-role"
                  className="h-9 w-full rounded-md border border-border/70 bg-white/80 px-2 text-sm"
                  value={createForm.role}
                  onChange={(event) =>
                    setCreateForm((prev) => ({
                      ...prev,
                      role: event.target.value as CreateUserRequest["role"]
                    }))
                  }
                >
                  {roleOptions.map((role) => (
                    <option key={role} value={role}>{role}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <Button variant="ghost" onClick={() => setIsCreateOpen(false)} disabled={isCreating}>
                Cancel
              </Button>
              <Button onClick={handleCreateUser} disabled={isCreating}>
                {isCreating ? "Creating..." : "Create"}
              </Button>
            </div>
          </div>
        </div>
      ) : null}

      {resetUser ? (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          role="dialog"
          aria-modal="true"
          onClick={() => {
            if (!isResetting) {
              setResetUser(null);
            }
          }}
        >
          <div
            className="w-full max-w-md rounded-2xl border border-border bg-background p-6 shadow-xl"
            onClick={(event) => event.stopPropagation()}
          >
            <h3 className="text-lg font-semibold">Reset password</h3>
            <p className="mt-2 text-sm text-muted-foreground">Set a new password for {resetUser.email}.</p>
            <div className="mt-4 space-y-1">
              <Label htmlFor="reset-password">New password</Label>
              <Input
                id="reset-password"
                type="password"
                value={resetPassword}
                onChange={(event) => setResetPassword(event.target.value)}
                placeholder="At least 8 characters"
              />
            </div>
            <div className="mt-6 flex justify-end gap-2">
              <Button variant="ghost" onClick={() => setResetUser(null)} disabled={isResetting}>
                Cancel
              </Button>
              <Button onClick={handleResetPassword} disabled={isResetting}>
                {isResetting ? "Resetting..." : "Reset password"}
              </Button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
