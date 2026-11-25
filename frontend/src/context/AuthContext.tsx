import React, { createContext, useContext, useEffect, useState } from 'react'
import { apiClient, tokenStorage } from '../api/client'
import type { AuthResponse, UserProfile } from '../types/api'

interface AuthContextValue {
  user: UserProfile | null
  isLoading: boolean
  isAuthenticated: boolean
  setSession: (auth: AuthResponse) => void
  refreshProfile: () => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  const refreshProfile = async () => {
    const token = tokenStorage.getAccessToken()
    if (!token) {
      setUser(null)
      setIsLoading(false)
      return
    }

    try {
      const res = await apiClient.get<UserProfile>('/profile')
      setUser(res.data)
    } catch {
      tokenStorage.clear()
      setUser(null)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void refreshProfile()
  }, [])

  const setSession = (auth: AuthResponse) => {
    tokenStorage.setTokens(auth.accessToken, auth.refreshToken)
    setUser(auth.user)
  }

  const logout = async () => {
    const refreshToken = tokenStorage.getRefreshToken()
    if (refreshToken) {
      try {
        await apiClient.post('/auth/revoke', { refreshToken })
      } catch {
        // ignore revoke errors on logout
      }
    }
    tokenStorage.clear()
    setUser(null)
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated: !!user,
        setSession,
        refreshProfile,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
