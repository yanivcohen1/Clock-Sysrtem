import React, { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { attendanceService } from '../services/attendanceService';
import type { EmployeeDto, AuditLogEntry } from '../types';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { Shield, Users, FileText, UserPlus, Pencil, Trash2, Key, X, Plus, ChevronLeft, ChevronRight } from 'lucide-react';
import { format } from 'date-fns';

export const AdminPage: React.FC = () => {
  const [tab, setTab] = useState<'employees' | 'audit'>('employees');
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [empTotal, setEmpTotal] = useState(0);
  const [empPage, setEmpPage] = useState(1);
  const pageSize = 10;
  const [auditLogs, setAuditLogs] = useState<AuditLogEntry[]>([]);
  const [auditTotal, setAuditTotal] = useState(0);
  const [auditPage, setAuditPage] = useState(1);
  const [auditPageSize, setAuditPageSize] = useState(10);
  const [loading, setLoading] = useState(true);
  const [showAddForm, setShowAddForm] = useState(false);
  const [resetPwdId, setResetPwdId] = useState<number | null>(null);
  const [actionError, setActionError] = useState('');
  const [newEmp, setNewEmp] = useState({ firstName: '', lastName: '', email: '', password: '', departmentId: 0, role: 'Employee', managerId: 0 });
  const [editingEmp, setEditingEmp] = useState<EmployeeDto | null>(null);
  const [editForm, setEditForm] = useState({ firstName: '', lastName: '', email: '', departmentId: 0, role: '', managerId: 0, isActive: true });

  const fetchData = async () => {
    setLoading(true); setActionError('');
    try {
      if (tab === 'employees') {
        const d = await attendanceService.getEmployees(empPage, pageSize);
        setEmployees(d.items); setEmpTotal(d.totalCount);
      }
      else { const d = await attendanceService.getAuditLog(auditPage); setAuditLogs(d.logs); setAuditTotal(d.total); setAuditPageSize(d.pageSize); }
    } catch { /* handled */ } finally { setLoading(false); }
  };
  useEffect(() => { fetchData(); }, [tab, auditPage, empPage]);

  const openEdit = (emp: EmployeeDto) => {
    setEditingEmp(emp);
    setEditForm({
      firstName: emp.fullName.split(' ')[0] || '',
      lastName: emp.fullName.split(' ').slice(1).join(' ') || '',
      email: emp.email,
      departmentId: 0,
      role: emp.role,
      managerId: 0,
      isActive: emp.isActive,
    });
    setActionError('');
  };

  const handleSaveEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingEmp) return;
    setActionError('');
    try {
      const updated = await attendanceService.updateEmployee(editingEmp.id, {
        firstName: editForm.firstName || undefined,
        lastName: editForm.lastName || undefined,
        email: editForm.email || undefined,
        departmentId: editForm.departmentId || undefined,
        role: editForm.role || undefined,
        managerId: editForm.managerId || undefined,
        isActive: editForm.isActive,
      });
      setEmployees(p => p.map(e => e.id === updated.id ? updated : e));
      setEditingEmp(null);
    } catch (err: any) {
      setActionError(err.response?.data?.error || 'Failed');
    }
  };
  const handleDelete = async (emp: EmployeeDto) => {
    if (!confirm(`Delete ${emp.fullName} permanently?`)) return;
    try { await attendanceService.deleteEmployee(emp.id); setEmployees(p => p.filter(e => e.id !== emp.id)); }
    catch (e: any) { setActionError(e.response?.data?.error || 'Failed'); }
  };
  const handleAddEmployee = async (e: React.FormEvent) => {
    e.preventDefault(); setActionError('');
    try { await attendanceService.createEmployee({ ...newEmp, departmentId: newEmp.departmentId || undefined, managerId: newEmp.managerId || undefined }); setShowAddForm(false); setNewEmp({ firstName: '', lastName: '', email: '', password: '', departmentId: 0, role: 'Employee', managerId: 0 }); fetchData(); }
    catch (err: any) { setActionError(err.response?.data?.error || 'Failed'); }
  };
  const handleResetPassword = async (empId: number, pwd: string) => {
    try { await attendanceService.adminResetPassword(empId, pwd); setResetPwdId(null); alert('Password reset.'); }
    catch (e: any) { setActionError(e.response?.data?.error || 'Failed'); }
  };

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2"><Shield className="h-6 w-6 text-primary-600" />Admin Panel</h1>
          <p className="text-gray-500 text-sm mt-1">System administration & audit</p>
        </div>
        {tab === 'employees' && (
          <button onClick={() => setShowAddForm(true)} className="flex items-center gap-2 bg-primary-600 hover:bg-primary-700 text-white px-4 py-2 rounded-lg text-sm font-medium"><UserPlus className="h-4 w-4" />Add Employee</button>
        )}
      </div>
      {actionError && (<div className="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700 flex justify-between">{actionError}<button onClick={() => setActionError('')}><X className="h-4 w-4" /></button></div>)}

      {/* Add Employee Modal — rendered via Portal to escape parent clipping */}
      {showAddForm && createPortal(
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[9999]" onClick={() => setShowAddForm(false)}>
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md mx-4" onClick={e => e.stopPropagation()}>
            <div className="flex justify-between mb-4"><h3 className="text-lg font-semibold">Add New Employee</h3><button onClick={() => setShowAddForm(false)}><X className="h-5 w-5 text-gray-400" /></button></div>
            <form onSubmit={handleAddEmployee} className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div><label className="block text-xs font-medium text-gray-600 mb-1">First Name *</label><input required value={newEmp.firstName} onChange={e => setNewEmp({ ...newEmp, firstName: e.target.value })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none" /></div>
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Last Name *</label><input required value={newEmp.lastName} onChange={e => setNewEmp({ ...newEmp, lastName: e.target.value })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none" /></div>
              </div>
              <div><label className="block text-xs font-medium text-gray-600 mb-1">Email *</label><input required type="email" value={newEmp.email} onChange={e => setNewEmp({ ...newEmp, email: e.target.value })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none" /></div>
              <div><label className="block text-xs font-medium text-gray-600 mb-1">Password *</label><input required minLength={6} value={newEmp.password} onChange={e => setNewEmp({ ...newEmp, password: e.target.value })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none" /></div>
              <div className="grid grid-cols-2 gap-3">
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Department</label><select value={newEmp.departmentId} onChange={e => setNewEmp({ ...newEmp, departmentId: Number(e.target.value) })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white"><option value={0}>— Optional —</option><option value={1}>Engineering</option><option value={2}>HR</option><option value={3}>Marketing</option><option value={4}>Finance</option><option value={5}>Operations</option></select></div>
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Role</label><select value={newEmp.role} onChange={e => setNewEmp({ ...newEmp, role: e.target.value, managerId: 0 })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white"><option>Employee</option><option>Manager</option><option>Admin</option></select></div>
              </div>
              {(newEmp.role === 'Employee' || newEmp.role === 'Manager') && (
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Manager{newEmp.role === 'Employee' ? ' *' : ''}</label><select required={newEmp.role === 'Employee'} value={newEmp.managerId} onChange={e => setNewEmp({ ...newEmp, managerId: Number(e.target.value) })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white"><option value={0}>— {newEmp.role === 'Employee' ? 'Select Manager' : 'Optional'} —</option>{employees.filter(e => e.role === 'Manager' || e.role === 'Admin').map(m => (<option key={m.id} value={m.id}>{m.fullName} ({m.role})</option>))}</select></div>
              )}
              <button type="submit" className="w-full flex items-center justify-center gap-2 bg-primary-600 hover:bg-primary-700 text-white font-semibold py-2.5 rounded-lg"><Plus className="h-4 w-4" />Create Employee</button>
            </form>
          </div>
        </div>,
        document.body
      )}

      {/* Edit Employee Modal */}
      {editingEmp && createPortal(
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-[9999]" onClick={() => setEditingEmp(null)}>
          <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md mx-4" onClick={e => e.stopPropagation()}>
            <div className="flex justify-between mb-4"><h3 className="text-lg font-semibold">Edit Employee</h3><button onClick={() => setEditingEmp(null)}><X className="h-5 w-5 text-gray-400" /></button></div>
            <form onSubmit={handleSaveEdit} className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div><label className="block text-xs font-medium text-gray-600 mb-1">First Name</label><input value={editForm.firstName} onChange={e => setEditForm({ ...editForm, firstName: e.target.value })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none" /></div>
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Last Name</label><input value={editForm.lastName} onChange={e => setEditForm({ ...editForm, lastName: e.target.value })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none" /></div>
              </div>
              <div><label className="block text-xs font-medium text-gray-600 mb-1">Email</label><input type="email" value={editForm.email} onChange={e => setEditForm({ ...editForm, email: e.target.value })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-primary-500 focus:ring-1 focus:ring-primary-500 outline-none" /></div>
              <div className="grid grid-cols-2 gap-3">
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Department</label><select value={editForm.departmentId} onChange={e => setEditForm({ ...editForm, departmentId: Number(e.target.value) })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white"><option value={0}>— Unchanged —</option><option value={1}>Engineering</option><option value={2}>HR</option><option value={3}>Marketing</option><option value={4}>Finance</option><option value={5}>Operations</option></select></div>
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Role</label><select value={editForm.role} onChange={e => setEditForm({ ...editForm, role: e.target.value, managerId: 0 })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white"><option>Employee</option><option>Manager</option><option>Admin</option></select></div>
              </div>
              {(editForm.role === 'Employee' || editForm.role === 'Manager') && (
                <div><label className="block text-xs font-medium text-gray-600 mb-1">Manager</label><select value={editForm.managerId} onChange={e => setEditForm({ ...editForm, managerId: Number(e.target.value) })} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm bg-white"><option value={0}>— Unchanged —</option>{employees.filter(e => e.role === 'Manager' || e.role === 'Admin').map(m => (<option key={m.id} value={m.id}>{m.fullName} ({m.role})</option>))}</select></div>
              )}
              <div className="flex items-center justify-between bg-gray-50 rounded-lg p-3">
                <span className="text-sm font-medium text-gray-700">Status</span>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input type="checkbox" checked={editForm.isActive} onChange={e => setEditForm({ ...editForm, isActive: e.target.checked })} className="sr-only peer" />
                  <div className="w-11 h-6 bg-gray-300 peer-focus:outline-none peer-focus:ring-2 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-500"></div>
                  <span className={`ml-3 text-sm font-medium ${editForm.isActive ? 'text-green-700' : 'text-red-700'}`}>{editForm.isActive ? 'Active' : 'Inactive'}</span>
                </label>
              </div>
              <button type="submit" className="w-full flex items-center justify-center gap-2 bg-primary-600 hover:bg-primary-700 text-white font-semibold py-2.5 rounded-lg"><Pencil className="h-4 w-4" />Save Changes</button>
            </form>
          </div>
        </div>,
        document.body
      )}

      {/* Tabs */}
      <div className="flex gap-2">
        {([{ key: 'employees' as const, label: 'Employees', icon: Users }, { key: 'audit' as const, label: 'Audit Log', icon: FileText }]).map(t => (
          <button key={t.key} onClick={() => setTab(t.key)} className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium ${tab === t.key ? 'bg-primary-600 text-white' : 'bg-white text-gray-600 border border-gray-200 hover:bg-gray-50'}`}><t.icon className="h-4 w-4" />{t.label}</button>
        ))}
      </div>
      {loading && <LoadingSpinner />}

      {/* Employees */}
      {!loading && tab === 'employees' && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="overflow-x-auto"><table className="w-full text-sm"><thead className="bg-gray-50"><tr>
            <th className="text-left px-4 py-3 font-medium text-gray-600">Employee</th><th className="text-left px-4 py-3 font-medium text-gray-600">Email</th><th className="text-left px-4 py-3 font-medium text-gray-600">Dept</th><th className="text-left px-4 py-3 font-medium text-gray-600">Role</th><th className="text-left px-4 py-3 font-medium text-gray-600">Manager</th><th className="text-left px-4 py-3 font-medium text-gray-600">Status</th><th className="text-left px-4 py-3 font-medium text-gray-600">Actions</th>
          </tr></thead><tbody>
            {employees.map(emp => (<tr key={emp.id} className="border-b border-gray-200 table-row-hover">
              <td className="px-4 py-3"><span className="font-medium">{emp.fullName}</span><span className="text-gray-400 ml-1 text-xs">({emp.employeeCode})</span></td>
              <td className="px-4 py-3 text-gray-600 text-xs">{emp.email}</td><td className="px-4 py-3 text-xs">{emp.department}</td>
              <td className="px-4 py-3"><span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${emp.role==='Admin'?'bg-purple-100 text-purple-800':emp.role==='Manager'?'bg-blue-100 text-blue-800':'bg-gray-100 text-gray-800'}`}>{emp.role}</span></td>
              <td className="px-4 py-3 text-xs text-gray-500">{emp.managerName || '—'}</td>
              <td className="px-4 py-3"><span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${emp.isActive?'bg-green-100 text-green-800':'bg-red-100 text-red-800'}`}>{emp.isActive?'Active':'Inactive'}</span></td>
              <td className="px-4 py-3"><div className="flex items-center gap-1.5">
                <button onClick={() => openEdit(emp)} title="Edit" className="p-1.5 rounded-lg text-primary-600 hover:bg-primary-50"><Pencil className="h-4 w-4" /></button>
                {resetPwdId === emp.id ? (
                  <InlineReset onReset={p => handleResetPassword(emp.id, p)} onCancel={() => setResetPwdId(null)} />
                ) : (<button onClick={() => setResetPwdId(emp.id)} title="Reset Password" className="p-1.5 rounded-lg text-blue-600 hover:bg-blue-50"><Key className="h-4 w-4" /></button>)}
                <button onClick={() => handleDelete(emp)} title="Delete" className="p-1.5 rounded-lg text-red-600 hover:bg-red-50"><Trash2 className="h-4 w-4" /></button>
              </div></td>
            </tr>))}
          </tbody></table></div>
          {empTotal > pageSize && (
            <div className="px-6 py-3 bg-gray-50 border-t border-gray-200 flex items-center justify-between">
              <span className="text-sm text-gray-500">Page {empPage} of {Math.ceil(empTotal / pageSize)} ({empTotal} total)</span>
              <div className="flex gap-2">
                <button onClick={() => setEmpPage(p => Math.max(1, p - 1))} disabled={empPage <= 1} className="flex items-center gap-1 px-3 py-1.5 text-sm rounded-md border border-gray-300 bg-white hover:bg-gray-50 disabled:opacity-40"><ChevronLeft className="h-4 w-4" />Prev</button>
                <button onClick={() => setEmpPage(p => p + 1)} disabled={empPage * pageSize >= empTotal} className="flex items-center gap-1 px-3 py-1.5 text-sm rounded-md border border-gray-300 bg-white hover:bg-gray-50 disabled:opacity-40">Next<ChevronRight className="h-4 w-4" /></button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Audit */}
      {!loading && tab === 'audit' && (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden"><div className="overflow-x-auto"><table className="w-full text-sm"><thead className="bg-gray-50"><tr>
          <th className="text-left px-4 py-3 font-medium text-gray-600">Timestamp</th><th className="text-left px-4 py-3 font-medium text-gray-600">Entity</th><th className="text-left px-4 py-3 font-medium text-gray-600">Action</th><th className="text-left px-4 py-3 font-medium text-gray-600">By</th><th className="text-left px-4 py-3 font-medium text-gray-600">Details</th>
        </tr></thead><tbody>
          {auditLogs.map(log => (<tr key={log.id} className="border-b border-gray-200 table-row-hover">
            <td className="px-4 py-3 text-xs text-gray-500 whitespace-nowrap">{format(new Date(log.timestamp), 'MMM dd, HH:mm:ss')}</td>
            <td className="px-4 py-3 font-medium text-xs">{log.entityName} #{log.entityId}</td>
            <td className="px-4 py-3"><span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${log.action==='ClockIn'?'bg-green-100 text-green-800':log.action==='ClockOut'?'bg-red-100 text-red-800':log.action==='AdminAdjustment'||log.action==='DeleteEmployee'?'bg-yellow-100 text-yellow-800':log.action==='ActivateEmployee'||log.action==='AdminCreateEmployee'?'bg-blue-100 text-blue-800':log.action==='DeactivateEmployee'?'bg-orange-100 text-orange-800':'bg-gray-100 text-gray-800'}`}>{log.action}</span></td>
            <td className="px-4 py-3 text-gray-600 text-xs">Emp #{log.performedByEmployeeId ?? '—'}</td>
            <td className="px-4 py-3">{log.newValue && (<details className="text-xs"><summary className="text-primary-600 cursor-pointer hover:underline">View</summary><pre className="mt-1 p-2 bg-gray-50 rounded text-xs overflow-x-auto max-w-xs">{JSON.stringify(JSON.parse(log.newValue), null, 2)}</pre></details>)}</td>
          </tr>))}
        </tbody></table></div>
        {auditTotal > auditPageSize && (<div className="px-6 py-3 bg-gray-50 border-t border-gray-200 flex items-center justify-between"><span className="text-sm text-gray-500">Page {auditPage} of {Math.ceil(auditTotal / auditPageSize)}</span><div className="flex gap-2"><button onClick={() => setAuditPage(p => Math.max(1, p - 1))} disabled={auditPage <= 1} className="flex items-center gap-1 px-3 py-1.5 text-sm rounded-md border border-gray-300 bg-white hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"><ChevronLeft className="h-4 w-4" />Prev</button><button onClick={() => setAuditPage(p => p + 1)} disabled={auditPage * auditPageSize >= auditTotal} className="flex items-center gap-1 px-3 py-1.5 text-sm rounded-md border border-gray-300 bg-white hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">Next<ChevronRight className="h-4 w-4" /></button></div></div>)}
        </div>
      )}
    </div>
  );
};

const InlineReset: React.FC<{ onReset: (p: string) => void; onCancel: () => void }> = ({ onReset, onCancel }) => {
  const [v, setV] = useState('');
  return (<form onSubmit={e => { e.preventDefault(); if (v.length >= 6) onReset(v); }} className="flex items-center gap-1"><input value={v} onChange={e => setV(e.target.value)} placeholder="New pwd" className="w-24 rounded border px-2 py-1 text-xs" /><button type="submit" className="text-xs bg-primary-600 text-white px-2 py-1 rounded">Save</button><button type="button" onClick={onCancel} className="text-xs text-gray-400">✕</button></form>);
};
