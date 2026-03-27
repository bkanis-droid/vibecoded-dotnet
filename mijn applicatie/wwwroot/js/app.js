const API_URL = '/api';

// Load data on page load
document.addEventListener('DOMContentLoaded', () => {
    loadPatients();
    loadAppointments();
});

// ========== TAB NAVIGATION ==========
function showTab(tabName) {
    // Hide all tabs
    document.querySelectorAll('.tab').forEach(tab => {
        tab.classList.remove('active');
    });
    
    // Remove active from all buttons
    document.querySelectorAll('.nav-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    
    // Show selected tab
    document.getElementById(tabName).classList.add('active');
    
    // Add active to clicked button
    event.target.classList.add('active');
    
    // Load specific data
    if (tabName === 'appointments') {
        loadAppointments();
    } else if (tabName === 'history') {
        // Clear history on tab switch
        document.getElementById('historyTableBody').innerHTML = 
            '<tr class="loading"><td colspan="6">Voer patiënt ID in</td></tr>';
    }
}

// ========== ALERT SYSTEM ==========
function showAlert(message, type = 'success') {
    const alert = document.getElementById('alert');
    alert.textContent = message;
    alert.className = `alert ${type} active`;
    
    setTimeout(() => {
        alert.classList.remove('active');
    }, 3000);
}

// ========== PATIENTS ==========
async function loadPatients() {
    try {
        const response = await fetch(`${API_URL}/patients`);
        const patients = await response.json();
        
        const tbody = document.getElementById('patientTableBody');
        
        if (!Array.isArray(patients) || patients.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" style="text-align: center;">Geen patiënten gevonden</td></tr>';
            return;
        }
        
        tbody.innerHTML = patients.map(p => `
            <tr>
                <td>${p.id}</td>
                <td>${p.firstName}</td>
                <td>${p.lastName}</td>
                <td>${p.email}</td>
                <td>${p.phoneNumber}</td>
                <td>${new Date(p.dateOfBirth).toLocaleDateString('nl-NL')}</td>
                <td>
                    <button class="btn btn-success" onclick="editPatient(${p.id})">Bewerken</button>
                    <button class="btn btn-danger" onclick="deletePatient(${p.id})">Verwijderen</button>
                </td>
            </tr>
        `).join('');
    } catch (error) {
        console.error('Error loading patients:', error);
        showAlert('Fout bij het laden van patiënten', 'danger');
    }
}

function openPatientModal() {
    document.getElementById('patientForm').reset();
    document.getElementById('patientId').value = '';
    document.getElementById('patientModalTitle').textContent = 'Nieuwe Patiënt';
    document.getElementById('patientModal').classList.add('active');
}

function closePatientModal() {
    document.getElementById('patientModal').classList.remove('active');
}

async function editPatient(id) {
    try {
        const response = await fetch(`${API_URL}/patients/${id}`);
        const patient = await response.json();
        
        document.getElementById('patientId').value = patient.id;
        document.getElementById('firstName').value = patient.firstName;
        document.getElementById('lastName').value = patient.lastName;
        document.getElementById('email').value = patient.email;
        document.getElementById('phone').value = patient.phoneNumber;
        document.getElementById('dob').value = patient.dateOfBirth.split('T')[0];
        document.getElementById('gender').value = patient.gender || '';
        
        document.getElementById('patientModalTitle').textContent = 'Patiënt Bewerken';
        document.getElementById('patientModal').classList.add('active');
    } catch (error) {
        console.error('Error loading patient:', error);
        showAlert('Fout bij het laden van patiënt', 'danger');
    }
}

async function savePatient(event) {
    event.preventDefault();
    
    const id = document.getElementById('patientId').value;
    const data = {
        firstName: document.getElementById('firstName').value,
        lastName: document.getElementById('lastName').value,
        email: document.getElementById('email').value,
        phoneNumber: document.getElementById('phone').value,
        dateOfBirth: document.getElementById('dob').value,
        gender: document.getElementById('gender').value
    };
    
    try {
        const method = id ? 'PUT' : 'POST';
        const url = id ? `${API_URL}/patients/${id}` : `${API_URL}/patients`;
        
        const response = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        
        if (!response.ok) throw new Error('Save failed');
        
        closePatientModal();
        loadPatients();
        showAlert(id ? 'Patiënt bijgewerkt' : 'Patiënt aangemaakt', 'success');
    } catch (error) {
        console.error('Error saving patient:', error);
        showAlert('Fout bij het opslaan van patiënt', 'danger');
    }
}

async function deletePatient(id) {
    if (!confirm('Zeker dat je deze patiënt wilt verwijderen?')) return;
    
    try {
        const response = await fetch(`${API_URL}/patients/${id}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) throw new Error('Delete failed');
        
        loadPatients();
        showAlert('Patiënt verwijderd', 'success');
    } catch (error) {
        console.error('Error deleting patient:', error);
        showAlert('Fout bij het verwijderen van patiënt', 'danger');
    }
}

function searchPatients() {
    const input = document.getElementById('patientSearch').value.toLowerCase();
    const rows = document.querySelectorAll('#patientTableBody tr');
    
    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        row.style.display = text.includes(input) ? '' : 'none';
    });
}

// ========== APPOINTMENTS ==========
async function loadAppointments() {
    try {
        const response = await fetch(`${API_URL}/appointments/date/${new Date().toISOString().split('T')[0]}`);
        const appointments = await response.json();
        
        const tbody = document.getElementById('appointmentTableBody');
        
        if (!Array.isArray(appointments) || appointments.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align: center;">Geen afspraken vandaag</td></tr>';
            return;
        }
        
        tbody.innerHTML = appointments.map(a => `
            <tr>
                <td>${a.id}</td>
                <td>${a.patientId}</td>
                <td>${new Date(a.appointmentDate).toLocaleString('nl-NL')}</td>
                <td>${a.reason || '-'}</td>
                <td><span class="status-badge status-${a.status.toLowerCase()}">${a.status}</span></td>
                <td>
                    <button class="btn btn-danger" onclick="deleteAppointment(${a.id})">Annuleren</button>
                </td>
            </tr>
        `).join('');
    } catch (error) {
        console.error('Error loading appointments:', error);
        showAlert('Fout bij het laden van afspraken', 'danger');
    }
}

function openAppointmentModal() {
    document.getElementById('appointmentForm').reset();
    document.getElementById('appointmentId').value = '';
    document.getElementById('appointmentModal').classList.add('active');
}

function closeAppointmentModal() {
    document.getElementById('appointmentModal').classList.remove('active');
}

async function saveAppointment(event) {
    event.preventDefault();
    
    const data = {
        patientId: parseInt(document.getElementById('appointmentPatientId').value),
        appointmentDate: new Date(document.getElementById('appointmentDate').value).toISOString(),
        reason: document.getElementById('appointmentReason').value,
        status: document.getElementById('appointmentStatus').value
    };
    
    try {
        const response = await fetch(`${API_URL}/appointments`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        
        if (!response.ok) throw new Error('Save failed');
        
        closeAppointmentModal();
        loadAppointments();
        showAlert('Afspraak aangemaakt', 'success');
    } catch (error) {
        console.error('Error saving appointment:', error);
        showAlert('Fout bij het aanmaken van afspraak', 'danger');
    }
}

async function deleteAppointment(id) {
    if (!confirm('Zeker dat je deze afspraak wilt annuleren?')) return;
    
    try {
        const response = await fetch(`${API_URL}/appointments/${id}`, {
            method: 'DELETE'
        });
        
        if (!response.ok) throw new Error('Delete failed');
        
        loadAppointments();
        showAlert('Afspraak geannuleerd', 'success');
    } catch (error) {
        console.error('Error deleting appointment:', error);
        showAlert('Fout bij het annuleren van afspraak', 'danger');
    }
}

// ========== MEDICAL HISTORY ==========
async function loadPatientHistory() {
    const patientId = document.getElementById('historyPatientId').value;
    
    if (!patientId) {
        document.getElementById('historyTableBody').innerHTML = 
            '<tr class="loading"><td colspan="6">Voer patiënt ID in</td></tr>';
        return;
    }
    
    try {
        const response = await fetch(`${API_URL}/medicalhistory/patient/${patientId}`);
        const histories = await response.json();
        
        const tbody = document.getElementById('historyTableBody');
        
        if (!Array.isArray(histories) || histories.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align: center;">Geen medische geschiedenis</td></tr>';
            return;
        }
        
        tbody.innerHTML = histories.map(h => `
            <tr>
                <td>${h.id}</td>
                <td>${h.patientId}</td>
                <td>${new Date(h.visitDate).toLocaleDateString('nl-NL')}</td>
                <td>${h.diagnosis || '-'}</td>
                <td>${h.medications || '-'}</td>
                <td>
                    <button class="btn btn-success" onclick="viewHistory(${h.id})">Details</button>
                </td>
            </tr>
        `).join('');
    } catch (error) {
        console.error('Error loading history:', error);
        showAlert('Fout bij het laden van medische geschiedenis', 'danger');
    }
}

function openHistoryModal() {
    document.getElementById('historyForm').reset();
    document.getElementById('historyModal').classList.add('active');
}

function closeHistoryModal() {
    document.getElementById('historyModal').classList.remove('active');
}

async function saveHistory(event) {
    event.preventDefault();
    
    const data = {
        patientId: parseInt(document.getElementById('historyPatientIdInput').value),
        visitDate: new Date(document.getElementById('visitDate').value).toISOString(),
        diagnosis: document.getElementById('diagnosis').value,
        treatment: document.getElementById('treatment').value,
        symptoms: document.getElementById('symptoms').value,
        medications: document.getElementById('medications').value
    };
    
    try {
        const response = await fetch(`${API_URL}/medicalhistory`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        
        if (!response.ok) throw new Error('Save failed');
        
        closeHistoryModal();
        loadPatientHistory();
        showAlert('Medisch record aangemaakt', 'success');
    } catch (error) {
        console.error('Error saving history:', error);
        showAlert('Fout bij het opslaan van medisch record', 'danger');
    }
}

async function viewHistory(id) {
    try {
        const response = await fetch(`${API_URL}/medicalhistory/${id}`);
        const history = await response.json();
        
        alert(`
Diagnose: ${history.diagnosis}
Behandeling: ${history.treatment}
Symptomen: ${history.symptoms}
Medicatie: ${history.medications}
        `);
    } catch (error) {
        console.error('Error loading history details:', error);
        showAlert('Fout bij het laden van details', 'danger');
    }
}

// Close modals when clicking outside
window.onclick = function(event) {
    const patientModal = document.getElementById('patientModal');
    const appointmentModal = document.getElementById('appointmentModal');
    const historyModal = document.getElementById('historyModal');
    
    if (event.target == patientModal) {
        patientModal.classList.remove('active');
    }
    if (event.target == appointmentModal) {
        appointmentModal.classList.remove('active');
    }
    if (event.target == historyModal) {
        historyModal.classList.remove('active');
    }
}
